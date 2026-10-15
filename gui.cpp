// =============================================================================
//  gui.cpp  --  Organizador de carpetas (version con VENTANA)
//
//  Usa el motor compartido (nucleo.hpp). Sin dependencias: solo Win32.
//  Compilar:  g++ -std=c++20 -O2 -municode -mwindows -o Organizador.exe gui.cpp
//             -lcomctl32 -lole32 -luuid -lshellapi -lshell32 -static
// =============================================================================

#include "nucleo.hpp"

#include <commctrl.h>
#include <shlobj.h>
#include <shellapi.h>

#include <string>

using namespace org;

// ------------------------------- IDs ----------------------------------------
#define ID_EDIT_RUTA   1001
#define ID_BTN_ELEGIR  1002
#define ID_CHK_FECHA   1003
#define ID_CHK_RECUR   1004
#define ID_LISTA       1005
#define ID_BTN_SIMULAR 1006
#define ID_BTN_APLICAR 1007
#define ID_BTN_DESHACER 1008
#define ID_BTN_ABRIR   1009
#define ID_PROGRESO    1010
#define ID_ESTADO      1011

// ------------------------------- estado -------------------------------------
static HWND  g_hwnd        = nullptr;
static HFONT g_font        = nullptr;
static HIMAGELIST g_imgSmall = nullptr;

static Options  g_opt;
static Plan     g_plan;
static fs::path g_ultimoLog;
static bool     g_listo = false;

// ------------------------- utilidades de la ventana -------------------------

static void setFont(HWND hijo, HFONT f) { SendMessageW(hijo, WM_SETFONT, (WPARAM)f, TRUE); }

static void estado(const std::wstring& texto) {
    SetWindowTextW(GetDlgItem(g_hwnd, ID_ESTADO), texto.c_str());
}

static bool elegirCarpeta(HWND owner, fs::path& salida) {
    IFileDialog* dlg = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_FileOpenDialog, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&dlg));
    if (FAILED(hr) || !dlg) return false;
    DWORD opts = 0;
    dlg->GetOptions(&opts);
    dlg->SetOptions(opts | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);
    dlg->SetTitle(L"Elige la carpeta que quieres organizar");
    bool ok = false;
    if (SUCCEEDED(dlg->Show(owner))) {
        IShellItem* item = nullptr;
        if (SUCCEEDED(dlg->GetResult(&item)) && item) {
            PWSTR p = nullptr;
            if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, &p)) && p) {
                salida = fs::path(p);
                ok = true;
                CoTaskMemFree(p);
            }
            item->Release();
        }
    }
    dlg->Release();
    return ok;
}

static int indiceIcono(const fs::path& p) {
    SHFILEINFOW sfi{};
    if (g_imgSmall && SHGetFileInfoW(p.c_str(), 0, &sfi, sizeof(sfi), SHGFI_SYSICONINDEX | SHGFI_SMALLICON))
        return sfi.iIcon;
    return -1;
}

static void ponerEstadoFila(int fila, int st) {
    LVITEMW it{};
    it.mask = LVIF_PARAM;
    it.iItem = fila;
    it.lParam = st;                       // 0 = pendiente, 1 = movido, 2 = error
    SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_SETITEMW, 0, (LPARAM)&it);
    RECT r{};
    ListView_GetItemRect(GetDlgItem(g_hwnd, ID_LISTA), fila, &r, LVIR_BOUNDS);
    InvalidateRect(GetDlgItem(g_hwnd, ID_LISTA), &r, TRUE);
}

static void ponerTexto(int fila, int col, const std::wstring& txt) {
    LVITEMW it{};
    it.mask = LVIF_TEXT;
    it.iItem = fila;
    it.iSubItem = col;
    it.pszText = (LPWSTR)txt.c_str();
    SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_SETITEMW, 0, (LPARAM)&it);
}

static void limpiarLista() {
    SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_DELETEALLITEMS, 0, 0);
}

// ------------------------------ simular -------------------------------------

static void simular() {
    HWND hLista = GetDlgItem(g_hwnd, ID_LISTA);
    limpiarLista();
    SendDlgItemMessageW(g_hwnd, ID_PROGRESO, PBM_SETPOS, 0, 0);
    g_ultimoLog.clear();

    std::error_code ec;
    if (g_opt.dir.empty() || !fs::exists(g_opt.dir, ec) || !fs::is_directory(g_opt.dir, ec)) {
        estado(L"Elige una carpeta válida para empezar.");
        EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), FALSE);
        g_listo = false;
        return;
    }

    g_plan = planificar(g_opt.dir, g_opt);
    g_listo = true;

    int fila = 0;
    for (const auto& m : g_plan.moves) {
        std::wstring nombre = m.from.filename().wstring();
        LVITEMW it{};
        it.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
        it.iItem = fila;
        it.iSubItem = 0;
        std::wstring nombreMostrar = nombre;
        it.pszText = (LPWSTR)nombreMostrar.c_str();
        it.iImage = indiceIcono(m.from);
        it.lParam = 0;
        int idx = (int)SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_INSERTITEMW, 0, (LPARAM)&it);
        if (idx < 0) { ++fila; continue; }

        FileInfo fi = infoDe(m.from);
        ponerTexto(idx, 1, fi.ok ? humanSize(fi.size) : L"—");
        ponerTexto(idx, 2, toWide(m.cat));
        std::error_code e2;
        fs::path rel = fs::relative(m.to, g_opt.dir, e2);
        ponerTexto(idx, 3, e2 ? m.to.wstring() : rel.wstring());
        ponerTexto(idx, 4, L"pendiente");
        ++fila;
    }

    for (int c = 0; c < 5; ++c) {
        ListView_SetColumnWidth(hLista, c, c == 3 ? 260 : (c == 0 ? 240 : (c == 4 ? 90 : 110)));
    }

    std::wstring res = std::to_wstring(g_plan.analyzed) + L" archivos · " +
                       std::to_wstring((long long)g_plan.moves.size()) + L" a clasificar";
    if (g_plan.skipped) res += L" · " + std::to_wstring(g_plan.skipped) + L" omitidos";
    if (g_plan.moves.empty()) res = L"Nada que organizar: la carpeta ya está en orden.";
    estado(res);

    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), g_plan.moves.empty() ? FALSE : TRUE);
}

// ------------------------------ aplicar -------------------------------------

static void aplicarTodo() {
    if (!g_listo || g_plan.moves.empty()) return;

    int total = (int)g_plan.moves.size();
    SendDlgItemMessageW(g_hwnd, ID_PROGRESO, PBM_SETRANGE32, 0, total);
    SendDlgItemMessageW(g_hwnd, ID_PROGRESO, PBM_SETPOS, 0, 0);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), FALSE);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SIMULAR), FALSE);

    int n = 0;
    ApplyResult r = aplicar(g_plan, g_opt.dir, [&](const Move& m, bool ok, const std::string& err) {
        (void)m; (void)err;
        if (n < total) {
            ponerEstadoFila(n, ok ? 1 : 2);
            ponerTexto(n, 4, ok ? L"movido" : L"error");
        }
        ++n;
        SendDlgItemMessageW(g_hwnd, ID_PROGRESO, PBM_SETPOS, n, 0);
    });

    // refrescar la columna de destino (puede haber cambiado por colisiones)
    for (size_t i = 0; i < r.done.size() && i < (size_t)total; ++i) {
        std::error_code e2;
        fs::path rel = fs::relative(r.done[i].to, g_opt.dir, e2);
        ponerTexto((int)i, 3, e2 ? r.done[i].to.wstring() : rel.wstring());
    }

    g_ultimoLog = r.log;
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_DESHACER), r.done.empty() ? FALSE : TRUE);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SIMULAR), TRUE);

    std::wstring res = L"Listo: " + std::to_wstring(r.moved) + L" movidos";
    if (r.errors) res += L", " + std::to_wstring(r.errors) + L" con error";
    res += L".  Registro: " + toWide(r.logName);
    estado(res);
}

static void deshacerUltimo() {
    if (g_ultimoLog.empty()) return;
    UndoResult r = deshacer(g_ultimoLog, nullptr);
    std::wstring res = L"Deshecho: " + std::to_wstring(r.restored) + L" restaurados";
    if (r.skipped) res += L", " + std::to_wstring(r.skipped) + L" omitidos";
    if (r.errors)  res += L", " + std::to_wstring(r.errors) + L" con error";
    estado(res);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_DESHACER), FALSE);
    simular();
}

static void abrirCarpeta() {
    std::error_code ec;
    if (g_opt.dir.empty() || !fs::exists(g_opt.dir, ec)) return;
    ShellExecuteW(g_hwnd, L"open", g_opt.dir.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
}

static void fijarCarpeta(const fs::path& p) {
    g_opt.dir = p;
    SetWindowTextW(GetDlgItem(g_hwnd, ID_EDIT_RUTA), p.wstring().c_str());
    simular();
}

// ------------------------------ layout --------------------------------------

static void colocarControles(HWND h, int ancho, int alto) {
    const int M = 12, F1 = 30, F2 = 30, BTN = 34, GAP = 8;
    int y = M;

    MoveWindow(GetDlgItem(h, ID_EDIT_RUTA),  M + 70, y, ancho - (M + 70) - (M + 150 + GAP), F1, TRUE);
    MoveWindow(GetDlgItem(h, ID_BTN_ELEGIR), ancho - M - 150, y, 150, F1, TRUE);
    y += F1 + GAP;

    MoveWindow(GetDlgItem(h, ID_CHK_FECHA), M, y, 260, 22, TRUE);
    MoveWindow(GetDlgItem(h, ID_CHK_RECUR), M + 270, y, 260, 22, TRUE);
    y += F2 + GAP;

    int altoInferior = 3 * BTN + 2 * GAP + 24;
    int altoLista = alto - y - altoInferior - M - 24;
    if (altoLista < 80) altoLista = 80;
    MoveWindow(GetDlgItem(h, ID_LISTA), M, y, ancho - 2 * M, altoLista, TRUE);
    y += altoLista + GAP;

    int anchoBtn = 130;
    MoveWindow(GetDlgItem(h, ID_BTN_SIMULAR),  M, y, anchoBtn, BTN, TRUE);
    MoveWindow(GetDlgItem(h, ID_BTN_APLICAR),  M + anchoBtn + GAP, y, anchoBtn, BTN, TRUE);
    MoveWindow(GetDlgItem(h, ID_BTN_DESHACER), M + 2 * (anchoBtn + GAP), y, anchoBtn, BTN, TRUE);
    MoveWindow(GetDlgItem(h, ID_BTN_ABRIR),    M + 3 * (anchoBtn + GAP), y, anchoBtn, BTN, TRUE);
    MoveWindow(GetDlgItem(h, ID_PROGRESO), M + 4 * (anchoBtn + GAP) + GAP, y + 6,
               ancho - (M + 4 * (anchoBtn + GAP) + GAP) - M, 22, TRUE);
    y += BTN + GAP;

    MoveWindow(GetDlgItem(h, ID_ESTADO), M, y, ancho - 2 * M, 20, TRUE);
}

// ------------------------------ WndProc -------------------------------------

static LRESULT CALLBACK WndProc(HWND h, UINT m, WPARAM w, LPARAM l) {
    switch (m) {
    case WM_CREATE: {
        HINSTANCE hi = (HINSTANCE)GetWindowLongPtrW(h, GWLP_HINSTANCE);

        CreateWindowExW(0, L"STATIC", L"Carpeta:", WS_CHILD | WS_VISIBLE | SS_LEFT,
                       0, 0, 60, 20, h, nullptr, hi, nullptr);
        CreateWindowExW(0, L"EDIT", L"", WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL | ES_READONLY,
                       0, 0, 100, 24, h, (HMENU)ID_EDIT_RUTA, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Elegir carpeta…", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
                       0, 0, 100, 24, h, (HMENU)ID_BTN_ELEGIR, hi, nullptr);

        CreateWindowExW(0, L"BUTTON", L"Agrupar por fecha (Año\\Mes)", WS_CHILD | WS_VISIBLE | BS_AUTOCHECKBOX,
                       0, 0, 100, 22, h, (HMENU)ID_CHK_FECHA, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Incluir subcarpetas", WS_CHILD | WS_VISIBLE | BS_AUTOCHECKBOX,
                       0, 0, 100, 22, h, (HMENU)ID_CHK_RECUR, hi, nullptr);

        CreateWindowExW(WS_EX_CLIENTEDGE, WC_LISTVIEWW, L"",
                       WS_CHILD | WS_VISIBLE | LVS_REPORT | LVS_SINGLESEL | LVS_SHOWSELALWAYS,
                       0, 0, 100, 100, h, (HMENU)ID_LISTA, hi, nullptr);
        HWND hl = GetDlgItem(h, ID_LISTA);
        ListView_SetExtendedListViewStyle(hl, LVS_EX_FULLROWSELECT | LVS_EX_DOUBLEBUFFER | LVS_EX_GRIDLINES);

        LVCOLUMNW col{};
        col.mask = LVCF_TEXT | LVCF_WIDTH | LVCF_SUBITEM;
        const wchar_t* titulos[] = { L"Archivo", L"Tamaño", L"Categoría", L"Destino", L"Estado" };
        for (int i = 0; i < 5; ++i) {
            col.iSubItem = i;
            col.pszText = (LPWSTR)titulos[i];
            col.cx = (i == 3) ? 260 : 110;
            SendMessageW(hl, LVM_INSERTCOLUMNW, i, (LPARAM)&col);
        }

        if (!g_imgSmall) {
            SHFILEINFOW sfi{};
            g_imgSmall = (HIMAGELIST)SHGetFileInfoW(L"C:\\", FILE_ATTRIBUTE_DIRECTORY, &sfi, sizeof(sfi),
                                                    SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
            if (g_imgSmall) {
                SendMessageW(hl, LVM_SETIMAGELIST, LVSIL_SMALL, (LPARAM)g_imgSmall);
            }
        }

        CreateWindowExW(0, L"BUTTON", L"Simular", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_SIMULAR, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Aplicar", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON | BS_DEFPUSHBUTTON,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_APLICAR, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Deshacer", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_DESHACER, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Abrir carpeta", WS_CHILD | WS_VISIBLE | BS_PUSHBUTTON,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_ABRIR, hi, nullptr);
        CreateWindowExW(0, PROGRESS_CLASSW, L"", WS_CHILD | WS_VISIBLE,
                       0, 0, 100, 22, h, (HMENU)ID_PROGRESO, hi, nullptr);
        CreateWindowExW(0, L"STATIC", L"", WS_CHILD | WS_VISIBLE | SS_LEFT,
                       0, 0, 100, 20, h, (HMENU)ID_ESTADO, hi, nullptr);

        // fuente Segoe UI para todo
        NONCLIENTMETRICSW ncm{};
        ncm.cbSize = sizeof(ncm);
        if (SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(ncm), &ncm, 0)) {
            g_font = CreateFontIndirectW(&ncm.lfMessageFont);
        } else {
            g_font = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
        }
        for (HWND hijo = GetWindow(h, GW_CHILD); hijo; hijo = GetWindow(hijo, GW_HWNDNEXT)) setFont(hijo, g_font);

        EnableWindow(GetDlgItem(h, ID_BTN_APLICAR), FALSE);
        EnableWindow(GetDlgItem(h, ID_BTN_DESHACER), FALSE);

        DragAcceptFiles(h, TRUE);
        return 0;
    }

    case WM_SIZE:
        colocarControles(h, LOWORD(l), HIWORD(l));
        return 0;

    case WM_COMMAND:
        switch (LOWORD(w)) {
        case ID_BTN_ELEGIR: {
            fs::path p;
            if (elegirCarpeta(h, p)) fijarCarpeta(p);
            return 0;
        }
        case ID_BTN_SIMULAR: simular(); return 0;
        case ID_BTN_APLICAR: aplicarTodo(); return 0;
        case ID_BTN_DESHACER: deshacerUltimo(); return 0;
        case ID_BTN_ABRIR: abrirCarpeta(); return 0;
        case ID_CHK_FECHA:
            g_opt.byDate = (SendMessageW((HWND)l, BM_GETCHECK, 0, 0) == BST_CHECKED);
            simular();
            return 0;
        case ID_CHK_RECUR:
            g_opt.recursive = (SendMessageW((HWND)l, BM_GETCHECK, 0, 0) == BST_CHECKED);
            simular();
            return 0;
        }
        return 0;

    case WM_NOTIFY: {
        LPNMHDR nh = (LPNMHDR)l;
        if (nh && nh->idFrom == ID_LISTA && nh->code == NM_CUSTOMDRAW) {
            LPNMLVCUSTOMDRAW cd = (LPNMLVCUSTOMDRAW)l;
            switch (cd->nmcd.dwDrawStage) {
            case CDDS_PREPAINT: return CDRF_NOTIFYITEMDRAW;
            case CDDS_ITEMPREPAINT: {
                LONG st = (LONG)cd->nmcd.lItemlParam;
                if (st == 1) { cd->clrText = RGB(0, 120, 0); }
                else if (st == 2) { cd->clrText = RGB(190, 0, 0); }
                return CDRF_NEWFONT;
            }
            }
            return CDRF_DODEFAULT;
        }
        return 0;
    }

    case WM_DROPFILES: {
        HDROP drop = (HDROP)w;
        wchar_t ruta[MAX_PATH * 2] = L"";
        if (DragQueryFileW(drop, 0, ruta, MAX_PATH * 2)) {
            std::error_code ec;
            fs::path p(ruta);
            if (fs::is_directory(p, ec)) fijarCarpeta(p);
        }
        DragFinish(drop);
        return 0;
    }

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(h, m, w, l);
}

// ------------------------------- main ---------------------------------------

int WINAPI wWinMain(HINSTANCE hi, HINSTANCE, PWSTR, int) {
    SetProcessDPIAware();

    INITCOMMONCONTROLSEX icc{ sizeof(icc), ICC_LISTVIEW_CLASSES | ICC_PROGRESS_CLASS | ICC_STANDARD_CLASSES };
    InitCommonControlsEx(&icc);
    CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);

    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hi;
    wc.lpszClassName = L"OrganizadorGUI";
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);

    // Icono incrustado en el .exe (recurso ID 1); si no, el generico de Windows
    HICON icoGrande = (HICON)LoadImageW(hi, MAKEINTRESOURCEW(1), IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_SHARED);
    HICON icoChico  = (HICON)LoadImageW(hi, MAKEINTRESOURCEW(1), IMAGE_ICON, 16, 16, LR_SHARED);
    wc.hIcon   = icoGrande ? icoGrande : LoadIcon(nullptr, IDI_APPLICATION);
    wc.hIconSm = icoChico  ? icoChico  : wc.hIcon;
    RegisterClassExW(&wc);

    g_hwnd = CreateWindowExW(0, L"OrganizadorGUI", L"Organizador de carpetas",
                             WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1000, 640,
                             nullptr, nullptr, hi, nullptr);
    if (!g_hwnd) return 1;

    ShowWindow(g_hwnd, SW_SHOW);
    UpdateWindow(g_hwnd);

    // carpeta por defecto: Descargas
    fijarCarpeta(defaultTargetDir());

    MSG msg;
    while (GetMessageW(&msg, nullptr, 0, 0) > 0) {
        if (!IsDialogMessageW(g_hwnd, &msg)) {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
    }

    if (g_font) DeleteObject(g_font);
    CoUninitialize();
    return 0;
}
