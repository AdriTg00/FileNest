// =============================================================================
//  gui.cpp  --  FileNet (Organizador de carpetas) - version con VENTANA
//
//  Usa el motor compartido (nucleo.hpp). Sin dependencias externas: solo Win32.
//  Incluye TEMA CLARO / OSCURO (boton "Tema"), persistido en el registro.
//
//  Compilar (ver build.bat):
//    g++ -std=c++20 -O2 -Wall -Wextra -municode -mwindows -static
//        -o "Organizador de carpetas.exe" gui.cpp recursos/icono.res.o
//        -lcomctl32 -lole32 -luuid -luxtheme -lshell32
// =============================================================================

#include "nucleo.hpp"
#include "recursos/recursos.h"

#include <commctrl.h>
#include <shlobj.h>
#include <shellapi.h>
#include <uxtheme.h>

#include <string>

using namespace org;

// ------------------------------- IDs ----------------------------------------
#define ID_EDIT_RUTA    1001
#define ID_BTN_ELEGIR   1002
#define ID_CHK_FECHA    1003
#define ID_CHK_RECUR    1004
#define ID_LISTA        1005
#define ID_BTN_SIMULAR  1006
#define ID_BTN_APLICAR  1007
#define ID_BTN_DESHACER 1008
#define ID_BTN_ABRIR    1009
#define ID_PROGRESO     1010
#define ID_ESTADO       1011
#define ID_BTN_TEMA     1012
#define ID_BTN_MOVIL    1013
#define ID_BTN_REGLAS   1014

// ------------------------------- paleta -------------------------------------
struct Paleta {
    COLORREF fondo;        // fondo de la ventana
    COLORREF panel;        // fondo de campos y lista
    COLORREF panel2;       // fondo alterno (barra de progreso, deshabilitado)
    COLORREF texto;        // texto normal
    COLORREF textoSuave;   // texto secundario / deshabilitado
    COLORREF borde;        // bordes y separadores
    COLORREF boton;        // fondo de boton
    COLORREF botonHover;   // fondo de boton al pasar/activo
    COLORREF botonTexto;   // texto de boton
    COLORREF acento;       // azul de marca
    COLORREF acentoTexto;  // texto sobre el acento
    COLORREF ok;           // fila movida
    COLORREF error;        // fila con error
};

static const Paleta P_CLARO = {
    RGB(240,244,248), RGB(255,255,255), RGB(228,235,242),
    RGB(28,38,49),    RGB(120,134,148), RGB(210,219,228),
    RGB(255,255,255), RGB(230,238,247), RGB(28,38,49),
    RGB(17,109,255),  RGB(255,255,255),
    RGB(0,128,60),    RGB(200,30,30)
};

static const Paleta P_OSCURO = {
    RGB(22,24,28),    RGB(32,35,41),    RGB(44,48,55),
    RGB(233,237,242), RGB(150,160,172), RGB(60,66,74),
    RGB(40,44,50),    RGB(56,62,70),    RGB(236,239,243),
    RGB(64,132,255),  RGB(255,255,255),
    RGB(94,214,140),  RGB(255,110,110)
};

static const Paleta* PAL = &P_CLARO;
static int      g_tema = 0;                 // 0 = claro, 1 = oscuro
static HBRUSH   g_brFondo  = nullptr;
static HBRUSH   g_brPanel  = nullptr;
static int      g_hover    = 0;             // id del boton bajo el raton

// ------------------------------- estado -------------------------------------
static HWND  g_hwnd    = nullptr;
static HFONT g_font    = nullptr;
static HIMAGELIST g_imgSmall = nullptr;

static Options  g_opt;
static Plan     g_plan;
static fs::path g_ultimoLog;
static bool     g_listo = false;

// --------------------------- utilidades internas ----------------------------

static void setFont(HWND hijo, HFONT f) { SendMessageW(hijo, WM_SETFONT, (WPARAM)f, TRUE); }

static void estado(const std::wstring& texto) {
    SetWindowTextW(GetDlgItem(g_hwnd, ID_ESTADO), texto.c_str());
    InvalidateRect(GetDlgItem(g_hwnd, ID_ESTADO), nullptr, TRUE);
}

// --------------------------- tema (persistencia) ----------------------------

static void guardarTema() {
    HKEY k = nullptr;
    DWORD v = (DWORD)g_tema;
    if (RegCreateKeyExW(HKEY_CURRENT_USER, L"Software\\FileNet", 0, nullptr,
                        REG_OPTION_NON_VOLATILE, KEY_WRITE, nullptr, &k, nullptr) == ERROR_SUCCESS) {
        RegSetValueExW(k, L"Tema", 0, REG_DWORD, (const BYTE*)&v, sizeof(v));
        RegCloseKey(k);
    }
}

static void cargarTema() {
    HKEY k = nullptr;
    DWORD v = 0, n = sizeof(v), tipo = 0;
    if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\FileNet", 0, KEY_READ, &k) == ERROR_SUCCESS) {
        if (RegQueryValueExW(k, L"Tema", nullptr, &tipo, (LPBYTE)&v, &n) == ERROR_SUCCESS && tipo == REG_DWORD)
            g_tema = (v == 1) ? 1 : 0;
        RegCloseKey(k);
    }
}

// ---------------------- registro: cadenas (ultima carpeta) ------------------

static void regEscribeCadena(const wchar_t* nombre, const std::wstring& valor) {
    HKEY k = nullptr;
    if (RegCreateKeyExW(HKEY_CURRENT_USER, L"Software\\FileNet", 0, nullptr,
                        REG_OPTION_NON_VOLATILE, KEY_WRITE, nullptr, &k, nullptr) == ERROR_SUCCESS) {
        RegSetValueExW(k, nombre, 0, REG_SZ, (const BYTE*)valor.c_str(),
                       (DWORD)((valor.size() + 1) * sizeof(wchar_t)));
        RegCloseKey(k);
    }
}

static std::wstring regLeeCadena(const wchar_t* nombre) {
    std::wstring v;
    HKEY k = nullptr;
    if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\FileNet", 0, KEY_READ, &k) == ERROR_SUCCESS) {
        DWORD n = 0, tipo = 0;
        if (RegQueryValueExW(k, nombre, nullptr, &tipo, nullptr, &n) == ERROR_SUCCESS &&
            tipo == REG_SZ && n >= sizeof(wchar_t)) {
            std::vector<wchar_t> buf(n / sizeof(wchar_t) + 1, 0);
            if (RegQueryValueExW(k, nombre, nullptr, nullptr, (LPBYTE)buf.data(), &n) == ERROR_SUCCESS)
                v = buf.data();
        }
        RegCloseKey(k);
    }
    return v;
}

// carpeta de datos de la app: %LOCALAPPDATA%\FileNet
static std::wstring carpetaDatos() {
    wchar_t base[32767] = L"";
    if (GetEnvironmentVariableW(L"LOCALAPPDATA", base, 32767) == 0) return L".";
    std::wstring d = std::wstring(base) + L"\\FileNet";
    CreateDirectoryW(d.c_str(), nullptr);
    return d;
}

static void tituloOscuro(HWND h, BOOL oscuro) {    HMODULE dw = LoadLibraryW(L"dwmapi.dll");
    if (!dw) return;
    typedef HRESULT (WINAPI *PFN)(HWND, DWORD, LPCVOID, DWORD);
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wcast-function-type"
    PFN f = (PFN)GetProcAddress(dw, "DwmSetWindowAttribute");
#pragma GCC diagnostic pop
    if (f) {
        BOOL v = oscuro;
        if (FAILED(f(h, 20, &v, sizeof(v)))) f(h, 19, &v, sizeof(v));   // 20 en Win10 20H1+, 19 antes
    }
    FreeLibrary(dw);
}

static void liberarPinceles() {
    if (g_brFondo) { DeleteObject(g_brFondo); g_brFondo = nullptr; }
    if (g_brPanel) { DeleteObject(g_brPanel); g_brPanel = nullptr; }
}

static void aplicarTema() {
    PAL = g_tema ? &P_OSCURO : &P_CLARO;
    liberarPinceles();
    g_brFondo = CreateSolidBrush(PAL->fondo);
    g_brPanel = CreateSolidBrush(PAL->panel);

    if (g_hwnd) {
        tituloOscuro(g_hwnd, g_tema == 1);

        const wchar_t* temaCtl = g_tema ? L"DarkMode_Explorer" : L"Explorer";

        HWND hl = GetDlgItem(g_hwnd, ID_LISTA);
        if (hl) {
            ListView_SetBkColor(hl, PAL->panel);
            ListView_SetTextBkColor(hl, PAL->panel);
            ListView_SetTextColor(hl, PAL->texto);
            SetWindowTheme(hl, temaCtl, nullptr);
        }
        HWND hp = GetDlgItem(g_hwnd, ID_PROGRESO);
        if (hp) InvalidateRect(hp, nullptr, TRUE);
        HWND he = GetDlgItem(g_hwnd, ID_EDIT_RUTA);
        if (he) SetWindowTheme(he, temaCtl, nullptr);

        SetWindowTextW(GetDlgItem(g_hwnd, ID_BTN_TEMA), g_tema ? L"Tema: oscuro" : L"Tema: claro");

        // repintar todo
        for (HWND h = GetWindow(g_hwnd, GW_CHILD); h; h = GetWindow(h, GW_HWNDNEXT)) {
            InvalidateRect(h, nullptr, TRUE);
        }
        InvalidateRect(g_hwnd, nullptr, TRUE);
        UpdateWindow(g_hwnd);
    }
}

// --------------------------- dibujo de controles ----------------------------

static void pintarBoton(LPDRAWITEMSTRUCT di) {
    HDC dc = di->hDC;
    RECT r = di->rcItem;
    bool pulsado = (di->itemState & ODS_SELECTED) != 0;
    bool deshab  = (di->itemState & ODS_DISABLED) != 0;
    bool foco    = (di->itemState & ODS_FOCUS) != 0;
    bool hover   = (g_hover == (int)di->CtlID) && !deshab;

    wchar_t txt[160] = L"";
    GetWindowTextW(di->hwndItem, txt, 160);

    COLORREF fondo = deshab ? PAL->panel2 : (pulsado ? PAL->acento : (hover ? PAL->botonHover : PAL->boton));
    COLORREF tcol  = deshab ? PAL->textoSuave : (pulsado ? PAL->acentoTexto : PAL->botonTexto);

    HBRUSH b = CreateSolidBrush(fondo);
    HPEN   p = CreatePen(PS_SOLID, 1, deshab ? PAL->borde : (pulsado ? PAL->acento : PAL->borde));
    HGDIOBJ ob = SelectObject(dc, b), op = SelectObject(dc, p);
    RoundRect(dc, r.left, r.top, r.right, r.bottom, 10, 10);
    SelectObject(dc, ob); SelectObject(dc, op);
    DeleteObject(b); DeleteObject(p);

    SetBkMode(dc, TRANSPARENT);
    SetTextColor(dc, tcol);
    DrawTextW(dc, txt, -1, &r, DT_CENTER | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);

    if (foco) {
        RECT f = r;
        InflateRect(&f, -3, -3);
        DrawFocusRect(dc, &f);
    }
}

static void pintarCheck(LPDRAWITEMSTRUCT di, bool marcado) {
    HDC dc = di->hDC;
    RECT r = di->rcItem;
    bool deshab = (di->itemState & ODS_DISABLED) != 0;

    wchar_t txt[160] = L"";
    GetWindowTextW(di->hwndItem, txt, 160);

    // rellenar el fondo con el color de la ventana
    FillRect(dc, &r, g_brFondo);

    int lado = 17;
    RECT c = r;
    c.left  = r.left + 1;
    c.top   = r.top + ((r.bottom - r.top) - lado) / 2;
    c.right = c.left + lado;
    c.bottom= c.top + lado;

    HBRUSH b = CreateSolidBrush(marcado ? PAL->acento : PAL->panel);
    HPEN   p = CreatePen(PS_SOLID, 1, marcado ? PAL->acento : PAL->borde);
    HGDIOBJ ob = SelectObject(dc, b), op = SelectObject(dc, p);
    RoundRect(dc, c.left, c.top, c.right, c.bottom, 5, 5);
    SelectObject(dc, ob); SelectObject(dc, op);
    DeleteObject(b); DeleteObject(p);

    if (marcado) {
        HPEN tick = CreatePen(PS_SOLID, 2, PAL->acentoTexto);
        HGDIOBJ ot = SelectObject(dc, tick);
        MoveToEx(dc, c.left + 4,  c.top + 9, nullptr);
        LineTo(dc,   c.left + 7,  c.top + 12);
        LineTo(dc,   c.right - 4, c.top + 5);
        SelectObject(dc, ot);
        DeleteObject(tick);
    }

    RECT t = r;
    t.left = c.right + 9;
    SetBkMode(dc, TRANSPARENT);
    SetTextColor(dc, deshab ? PAL->textoSuave : PAL->texto);
    DrawTextW(dc, txt, -1, &t, DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);
}

// --------------------------- barra de progreso ------------------------------

static int g_progHechos = 0, g_progTotal = 0;

static void refrescarProgreso() {
    InvalidateRect(GetDlgItem(g_hwnd, ID_PROGRESO), nullptr, FALSE);
}

static void pintarProgreso(LPDRAWITEMSTRUCT di) {
    HDC dc = di->hDC;
    RECT r = di->rcItem;
    FillRect(dc, &r, g_brFondo);

    RECT b = r;
    InflateRect(&b, -1, -1);
    if (b.right - b.left < 8 || b.bottom - b.top < 8) return;

    HBRUSH bc = CreateSolidBrush(PAL->panel2);
    HPEN   pc = CreatePen(PS_SOLID, 1, PAL->borde);
    HGDIOBJ ob = SelectObject(dc, bc), op = SelectObject(dc, pc);
    RoundRect(dc, b.left, b.top, b.right, b.bottom, 8, 8);
    SelectObject(dc, ob); SelectObject(dc, op);
    DeleteObject(bc); DeleteObject(pc);

    if (g_progTotal > 0 && g_progHechos > 0) {
        int ancho = b.right - b.left;
        int lleno = (int)((long long)ancho * g_progHechos / g_progTotal);
        if (lleno < 4) lleno = 4;
        HRGN rg = CreateRoundRectRgn(b.left, b.top, b.right, b.bottom, 8, 8);
        HRGN rf = CreateRectRgn(b.left, b.top, b.left + lleno, b.bottom);
        HRGN ri = CreateRectRgn(0, 0, 0, 0);
        if (CombineRgn(ri, rg, rf, RGN_AND) != ERROR) {
            HBRUSH ba = CreateSolidBrush(PAL->acento);
            FillRgn(dc, ri, ba);
            DeleteObject(ba);
        }
        DeleteObject(ri); DeleteObject(rf); DeleteObject(rg);
    }

    wchar_t txt[48] = L"";
    if (g_progTotal > 0) {
        swprintf(txt, 48, L"%d%%   %d/%d",
                 (int)(100LL * g_progHechos / g_progTotal), g_progHechos, g_progTotal);
    }
    if (txt[0]) {
        SetBkMode(dc, TRANSPARENT);
        SetTextColor(dc, g_progHechos >= g_progTotal ? PAL->acentoTexto : PAL->textoSuave);
        DrawTextW(dc, txt, -1, &r, DT_CENTER | DT_VCENTER | DT_SINGLELINE);
    }
}

// --------------------------- selector de carpeta ----------------------------

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

static std::map<std::wstring, int> g_iconos;   // extension -> indice de icono (cache)

static int indiceIcono(const fs::path& p) {
    if (!g_imgSmall) return -1;
    std::wstring ext = lowerW(p.extension().wstring());
    auto it = g_iconos.find(ext);
    if (it != g_iconos.end()) return it->second;

    // SHGetFileInfo es CARO (consulta el shell): se llama una vez por extension
    SHFILEINFOW sfi{};
    int idx = -1;
    if (SHGetFileInfoW(p.c_str(), 0, &sfi, sizeof(sfi), SHGFI_SYSICONINDEX | SHGFI_SMALLICON))
        idx = sfi.iIcon;
    g_iconos[ext] = idx;
    return idx;
}

static void ponerEstadoFila(int fila, int st) {
    LVITEMW it{};
    it.mask = LVIF_PARAM;
    it.iItem = fila;
    it.lParam = st;                       // 0 pendiente, 1 movido, 2 error
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

// --------------------------- movil por USB (adb) ---------------------------
//
//  FASE 1: solo DETECTA el movil y LISTA lo que organizaria en /sdcard/Download.
//  No mueve nada todavia. Requiere "Depuracion USB" activada en el movil.

static std::wstring g_adb;

static bool buscarAdb() {
    if (!g_adb.empty()) return true;

    wchar_t buf[MAX_PATH * 2];
    if (SearchPathW(nullptr, L"adb.exe", nullptr, MAX_PATH * 2, buf, nullptr)) { g_adb = buf; return true; }

    wchar_t base[32767];
    if (GetEnvironmentVariableW(L"LOCALAPPDATA", base, 32767) > 0) {
        std::wstring paquetes = std::wstring(base) + L"\\Microsoft\\WinGet\\Packages";
        WIN32_FIND_DATAW fd{};
        HANDLE h = FindFirstFileW((paquetes + L"\\Google.PlatformTools_*").c_str(), &fd);
        if (h != INVALID_HANDLE_VALUE) {
            do {
                if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
                    std::wstring c = paquetes + L"\\" + fd.cFileName + L"\\platform-tools\\adb.exe";
                    if (GetFileAttributesW(c.c_str()) != INVALID_FILE_ATTRIBUTES) {
                        g_adb = c; FindClose(h); return true;
                    }
                }
            } while (FindNextFileW(h, &fd));
            FindClose(h);
        }
    }
    return false;
}

// Ejecuta adb volcando la salida a un fichero temporal.
//  - NO usa tuberia: el proceso "daemon" de adb hereda los handles y una tuberia
//    se quedaria abierta para siempre (la ventana se congelaba).
//  - Con timeout: si adb no responde, se mata y se sigue.
static std::string ejecutarAdb(const std::wstring& args, DWORD timeoutMs = 12000) {
    std::string salida;
    if (!buscarAdb()) return salida;

    wchar_t carpeta[MAX_PATH] = L"";
    GetTempPathW(MAX_PATH, carpeta);
    wchar_t tmp[MAX_PATH] = L"";
    if (!GetTempFileNameW(carpeta, L"adn", 0, tmp)) return salida;

    std::wstring cmd = L"cmd.exe /c \"\"" + g_adb + L"\" " + args + L" > \"" + tmp + L"\" 2>&1\"";
    std::vector<wchar_t> linea(cmd.begin(), cmd.end());
    linea.push_back(0);

    STARTUPINFOW si{};
    si.cb = sizeof(si);
    PROCESS_INFORMATION pi{};
    if (CreateProcessW(nullptr, linea.data(), nullptr, nullptr, FALSE,
                       CREATE_NO_WINDOW, nullptr, nullptr, &si, &pi)) {
        if (WaitForSingleObject(pi.hProcess, timeoutMs) == WAIT_TIMEOUT) {
            TerminateProcess(pi.hProcess, 1);
            CloseHandle(pi.hProcess);
            CloseHandle(pi.hThread);
            DeleteFileW(tmp);
            return salida;                 // vacio = "adb no responde"
        }
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }

    {
        std::ifstream f(tmp, std::ios::binary);
        std::ostringstream ss;
        ss << f.rdbuf();
        salida = ss.str();
    }
    DeleteFileW(tmp);
    return salida;
}

static std::string recortar(std::string s) {
    while (!s.empty() && (s.back() == '\r' || s.back() == '\n' || s.back() == ' ')) s.pop_back();
    size_t i = 0;
    while (i < s.size() && (s[i] == ' ' || s[i] == '\r' || s[i] == '\n')) ++i;
    return s.substr(i);
}

static std::vector<std::string> lineas(const std::string& s) {
    std::vector<std::string> v;
    std::istringstream ss(s);
    std::string l;
    while (std::getline(ss, l)) { l = recortar(l); if (!l.empty()) v.push_back(l); }
    return v;
}

// --- FASE 2: organizar de verdad dentro del movil ---

struct MovMovil { std::string nombre; std::string cat; };

static std::vector<MovMovil> g_planMovil;
static bool        g_modoMovil = false;
static std::string g_movilId, g_movilCarpeta;
static std::wstring g_ultimoLogMovil;          // registro de la ultima organizacion en el movil
static bool         g_ultimoFueMovil = false;  // true = el boton Deshacer actua sobre el movil

static std::string idDispositivo(bool& adbRespondio);   // (definido mas abajo)
static bool        elegirCarpetaMovil(std::string& salida);  // (definido mas abajo)

// cita un texto para la shell del movil (comillas simples)
static std::string citaSH(const std::string& s) {
    std::string r = "'";
    for (char c : s) { if (c == '\'') r += "'\\''"; else r += c; }
    r += "'";
    return r;
}

static bool escribirArchivo(const std::wstring& ruta, const std::string& contenido) {
    std::ofstream f(ruta.c_str(), std::ios::binary);
    if (!f) return false;
    f.write(contenido.data(), (std::streamsize)contenido.size());
    return f.good();
}

static void aplicarMovil() {
    if (!g_modoMovil || g_planMovil.empty()) return;

    wchar_t tmpDir[MAX_PATH] = L"";
    GetTempPathW(MAX_PATH, tmpDir);
    std::wstring rutaSh      = std::wstring(tmpDir) + L"filenet_tarea.sh";
    std::wstring rutaRemota  = L"/data/local/tmp/filenet.sh";
    std::wstring wid         = toWide(g_movilId);

    // --- script para el movil ---
    std::string sh = "cd " + citaSH(g_movilCarpeta) + " || exit 1\n";

    std::set<std::string> cats;
    for (const auto& m : g_planMovil) cats.insert(m.cat);
    for (const auto& c : cats) sh += "mkdir -p " + citaSH(c) + " 2>/dev/null\n";

    for (const auto& m : g_planMovil) {
        sh += "mv " + citaSH(m.nombre) + " " + citaSH(m.cat + "/" + m.nombre) +
              " 2>/dev/null && printf 'OK\\t%s\\n' " + citaSH(m.nombre) +
              " || printf 'ERR\\t%s\\n' " + citaSH(m.nombre) + "\n";
    }
    if (!escribirArchivo(rutaSh, sh)) { estado(L"No se pudo preparar la tarea."); return; }

    // --- subir, ejecutar y borrar ---
    int total = (int)g_planMovil.size();
    g_progTotal = total; g_progHechos = 0; refrescarProgreso();

    ejecutarAdb(L"-s " + wid + L" push \"" + rutaSh + L"\" " + rutaRemota, 60000);
    std::string salida = ejecutarAdb(L"-s " + wid + L" shell sh " + rutaRemota, 180000);
    ejecutarAdb(L"-s " + wid + L" shell rm -f " + rutaRemota);
    DeleteFileW(rutaSh.c_str());

    // --- interpretar OK / ERR ---
    std::map<std::string, int> res;
    for (const auto& l : lineas(salida)) {
        if (l.rfind("OK\t", 0) == 0)       res[l.substr(3)] = 1;
        else if (l.rfind("ERR\t", 0) == 0) res[l.substr(4)] = 2;
    }

    int ok = 0, err = 0;
    for (int i = 0; i < total; ++i) {
        auto it = res.find(g_planMovil[i].nombre);
        int st = (it == res.end()) ? 2 : it->second;
        ponerEstadoFila(i, st);
        ponerTexto(i, 4, st == 1 ? L"movido" : L"error");
        if (st == 1) ++ok; else ++err;
        g_progHechos = i + 1;
        refrescarProgreso();
    }

    // registro para poder deshacer (se guarda en el PC, en %LOCALAPPDATA%\FileNet)
    if (ok > 0) {
        std::wstring rutaLog = carpetaDatos() + L"\\movil-" + toWide(nowStamp()) + L".csv";
        std::ofstream f(rutaLog.c_str(), std::ios::binary);
        if (f) {
            f << "accion,ruta_original,ruta_destino\r\n";
            for (int i = 0; i < total; ++i) {
                auto it = res.find(g_planMovil[i].nombre);
                if (it == res.end() || it->second != 1) continue;
                std::string origen  = g_movilCarpeta + "/" + g_planMovil[i].nombre;
                std::string destino = g_movilCarpeta + "/" + g_planMovil[i].cat + "/" + g_planMovil[i].nombre;
                f << "MOVE," << csvField(origen) << "," << csvField(destino) << "\r\n";
            }
        }
        g_ultimoLogMovil = rutaLog;
        g_ultimoFueMovil = true;
        EnableWindow(GetDlgItem(g_hwnd, ID_BTN_DESHACER), TRUE);
    }

    g_modoMovil = false;
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), FALSE);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SIMULAR), TRUE);

    std::wstring resumen = L"Móvil: " + std::to_wstring(ok) + L" movidos";
    if (err) resumen += L", " + std::to_wstring(err) + L" con error";
    resumen += L".   Pulsa «Deshacer» si te arrepientes.";
    estado(resumen);
}

// deshace la ultima organizacion hecha EN EL MOVIL (usa el registro del PC)
static void deshacerMovil() {
    if (g_ultimoLogMovil.empty()) return;

    bool respondio = false;
    std::string id = idDispositivo(respondio);
    if (id.empty()) {
        MessageBoxW(g_hwnd, respondio
            ? L"No hay ningún móvil conectado.\n\nConecta el mismo móvil y vuelve a intentarlo."
            : L"adb no ha respondido.",
            L"Deshacer en el móvil", MB_ICONWARNING);
        return;
    }
    if (MessageBoxW(g_hwnd, L"¿Devolver los archivos a su sitio DENTRO del móvil?",
                    L"Deshacer en el móvil", MB_ICONQUESTION | MB_YESNO) != IDYES) return;

    std::ifstream f(g_ultimoLogMovil.c_str(), std::ios::binary);
    if (!f) { estado(L"No encuentro el registro del móvil."); return; }
    std::ostringstream ss; ss << f.rdbuf();
    auto filas = parseCsv(ss.str());

    std::string sh;
    int n = 0;
    for (const auto& fila : filas) {
        if (fila.size() < 3 || fila[0] != "MOVE") continue;
        const std::string& origen  = fila[1];   // donde estaba
        const std::string& destino = fila[2];   // donde esta ahora
        sh += "mv " + citaSH(destino) + " " + citaSH(origen) +
              " 2>/dev/null && printf 'OK\\t%s\\n' " + citaSH(origen) +
              " || printf 'ERR\\t%s\\n' " + citaSH(origen) + "\n";
        ++n;
    }
    if (n == 0) { estado(L"El registro del móvil está vacío."); return; }

    wchar_t tmpDir[MAX_PATH] = L"";
    GetTempPathW(MAX_PATH, tmpDir);
    std::wstring rutaSh     = std::wstring(tmpDir) + L"filenet_deshacer.sh";
    std::wstring rutaRemota = L"/data/local/tmp/filenet_deshacer.sh";
    if (!escribirArchivo(rutaSh, sh)) { estado(L"No se pudo preparar la tarea."); return; }

    std::wstring wid = toWide(id);
    ejecutarAdb(L"-s " + wid + L" push \"" + rutaSh + L"\" " + rutaRemota, 60000);
    std::string salida = ejecutarAdb(L"-s " + wid + L" shell sh " + rutaRemota, 180000);
    ejecutarAdb(L"-s " + wid + L" shell rm -f " + rutaRemota);
    DeleteFileW(rutaSh.c_str());

    int ok = 0, err = 0;
    for (const auto& l : lineas(salida)) {
        if (l.rfind("OK\t", 0) == 0)       ++ok;
        else if (l.rfind("ERR\t", 0) == 0) ++err;
    }
    std::wstring r2 = L"Móvil: " + std::to_wstring(ok) + L" restaurados";
    if (err) r2 += L", " + std::to_wstring(err) + L" con error";
    r2 += L".";
    estado(r2);

    g_ultimoFueMovil = false;
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_DESHACER), FALSE);
}

// id del primer movil conectado (vacio si no hay). adbRespondio indica si adb contesto.
static std::string idDispositivo(bool& adbRespondio) {
    adbRespondio = false;
    std::string disp = ejecutarAdb(L"devices");
    if (disp.empty()) return "";
    adbRespondio = true;
    for (const auto& l : lineas(disp)) {
        if (l.find("List of devices") != std::string::npos) continue;
        if (l.find("device") != std::string::npos && l.find("unauthorized") == std::string::npos) {
            size_t p = l.find_first_of("\t ");
            return l.substr(0, p);
        }
    }
    return "";
}

static void verMovil() {
    if (!buscarAdb()) {
        MessageBoxW(g_hwnd,
            L"No encuentro adb.exe.\n\nSe instala con:\n    winget install Google.PlatformTools",
            L"Móvil", MB_ICONINFORMATION);
        return;
    }

    std::string id, modelo;
    bool respondio = false;
    id = idDispositivo(respondio);
    if (id.empty()) {
        MessageBoxW(g_hwnd, respondio
            ? L"No hay ningún móvil conectado (o está sin autorizar).\n\n"
              L"1) En el móvil: Ajustes > Opciones de desarrollador > Depuración USB (activar).\n"
              L"2) Conéctalo por USB y acepta el aviso «¿Permitir depuración USB?».\n"
              L"3) Vuelve a pulsar «Móvil»."
            : L"adb no ha respondido.\n\nComprueba en una consola que funciona:\n    adb devices",
            L"Móvil", MB_ICONWARNING);
        return;
    }

    std::wstring wid = toWide(id);
    g_movilId = id;                       // lo usa el selector de carpeta
    modelo = recortar(ejecutarAdb(L"-s " + wid + L" shell getprop ro.product.model"));

    // el usuario elige la carpeta del movil (con navegacion)
    std::string carpeta;
    if (!elegirCarpetaMovil(carpeta)) return;      // cancelado
    if (carpeta.empty()) carpeta = "/sdcard/Download";

    std::vector<std::string> entradas = lineas(ejecutarAdb(L"-s " + wid + L" shell ls -p " + toWide(carpeta)));

    SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_DELETEALLITEMS, 0, 0);
    g_planMovil.clear();
    g_movilId = id;
    g_movilCarpeta = carpeta;
    auto mapa = mapaExtensiones(g_opt);
    int fila = 0, nArchivos = 0;
    for (const auto& e : entradas) {
        if (!e.empty() && e.back() == '/') continue;         // fase 1: solo archivos
        std::string nombre = e;
        if (nombre.empty()) continue;
        std::string cat = categoriaDeNombre(mapa, nombre);
        g_planMovil.push_back({ nombre, cat });

        std::wstring wn = toWide(nombre);
        LVITEMW it{};
        it.mask = LVIF_TEXT | LVIF_PARAM;
        it.iItem = fila; it.iSubItem = 0;
        it.pszText = (LPWSTR)wn.c_str();
        it.lParam = 0;
        int idx = (int)SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_INSERTITEMW, 0, (LPARAM)&it);
        if (idx < 0) { ++fila; continue; }
        ponerTexto(idx, 1, L"—");
        ponerTexto(idx, 2, toWide(cat));
        ponerTexto(idx, 3, toWide(carpeta + "/" + cat + "/" + nombre));
        ponerTexto(idx, 4, L"visto");
        ++fila; ++nArchivos;
    }

    g_progHechos = 0; g_progTotal = 0; refrescarProgreso();
    g_listo = false;
    g_modoMovil = !g_planMovil.empty();
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), g_modoMovil ? TRUE : FALSE);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SIMULAR), TRUE);

    std::wstring res = L"Móvil: " + (modelo.empty() ? toWide(id) : toWide(modelo)) + L"   ·   " +
                       toWide(carpeta) + L"   ·   " + std::to_wstring(nArchivos) + L" archivos";
    res += g_planMovil.empty() ? L"   (nada que organizar)"
                               : L"   ·   pulsa «Aplicar» para organizarlos";
    estado(res);
}

// ------------------------------ acciones ------------------------------------

static void simular() {
    g_modoMovil = false;
    HWND hLista = GetDlgItem(g_hwnd, ID_LISTA);
    SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_DELETEALLITEMS, 0, 0);
    g_progHechos = 0; g_progTotal = 0;
    refrescarProgreso();
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

    SendDlgItemMessageW(g_hwnd, ID_LISTA, WM_SETREDRAW, FALSE, 0);   // sin repintar mientras rellenamos
    int fila = 0;
    for (const auto& m : g_plan.moves) {
        std::wstring nombreMostrar = m.from.filename().wstring();
        LVITEMW it{};
        it.mask = LVIF_TEXT | LVIF_IMAGE | LVIF_PARAM;
        it.iItem = fila;
        it.iSubItem = 0;
        it.pszText = (LPWSTR)nombreMostrar.c_str();
        it.iImage = indiceIcono(m.from);
        it.lParam = 0;
        int idx = (int)SendDlgItemMessageW(g_hwnd, ID_LISTA, LVM_INSERTITEMW, 0, (LPARAM)&it);
        if (idx < 0) { ++fila; continue; }

        FileInfo fi = infoDe(m.from);
        ponerTexto(idx, 1, fi.ok ? humanSize(fi.size) : L"—");
        ponerTexto(idx, 2, toWide(m.cat));
        // lexically_relative: puro texto, NO consulta el disco (fs::relative si, y era lentisimo)
        fs::path rel = m.to.lexically_relative(g_opt.dir);
        ponerTexto(idx, 3, rel.empty() ? m.to.wstring() : rel.wstring());
        ponerTexto(idx, 4, L"pendiente");
        ++fila;
    }

    SendDlgItemMessageW(g_hwnd, ID_LISTA, WM_SETREDRAW, TRUE, 0);
    InvalidateRect(hLista, nullptr, TRUE);
    UpdateWindow(hLista);

    std::wstring res = std::to_wstring(g_plan.analyzed) + L" archivos · " +
                       std::to_wstring((long long)g_plan.moves.size()) + L" a clasificar";
    if (g_plan.skipped) res += L" · " + std::to_wstring(g_plan.skipped) + L" omitidos";
    if (g_plan.moves.empty()) res = L"Nada que organizar: la carpeta ya está en orden.";
    estado(res);

    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_APLICAR), g_plan.moves.empty() ? FALSE : TRUE);
}

static void aplicarTodo() {
    if (g_modoMovil) {
        std::wstring msg = L"¿Mover " + std::to_wstring(g_planMovil.size()) +
                           L" archivos DENTRO del móvil?\n\n" + toWide(g_movilCarpeta) +
                           L"\n\nSe crearán subcarpetas por tipo y los archivos se moverán ahí.";
        if (MessageBoxW(g_hwnd, msg.c_str(), L"Móvil", MB_ICONQUESTION | MB_YESNO) == IDYES)
            aplicarMovil();
        return;
    }
    if (!g_listo || g_plan.moves.empty()) return;

    int total = (int)g_plan.moves.size();
    g_progTotal = total; g_progHechos = 0;
    refrescarProgreso();
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
        g_progHechos = n;
        refrescarProgreso();
    });

    for (size_t i = 0; i < r.done.size() && i < (size_t)total; ++i) {
        fs::path rel = r.done[i].to.lexically_relative(g_opt.dir);
        ponerTexto((int)i, 3, rel.empty() ? r.done[i].to.wstring() : rel.wstring());
    }

    g_ultimoLog = r.log;
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_DESHACER), r.done.empty() ? FALSE : TRUE);
    EnableWindow(GetDlgItem(g_hwnd, ID_BTN_SIMULAR), TRUE);

    std::wstring res = L"Listo: " + std::to_wstring(r.moved) + L" movidos";
    if (r.errors) res += L", " + std::to_wstring(r.errors) + L" con error";
    res += L".  Registro: " + toWide(r.logName);
    estado(res);
}

[[maybe_unused]] static void deshacerUltimo() {
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
    regEscribeCadena(L"UltimaCarpeta", p.wstring());   // se recuerda para la proxima vez
    SetWindowTextW(GetDlgItem(g_hwnd, ID_EDIT_RUTA), p.wstring().c_str());
    simular();
}

// ============================== dialogos ====================================

// ------------------------- historial de deshacer ----------------------------

static std::vector<std::pair<std::wstring, std::wstring>> g_logs;   // nombre, ruta
static std::wstring g_logElegido;

static void recogerLogs() {
    g_logs.clear();
    std::error_code ec;
    fs::directory_iterator it(g_opt.dir, ec);
    if (ec) return;
    for (const auto& e : it) {
        if (!e.is_regular_file(ec)) continue;
        std::wstring n = e.path().filename().wstring();
        if (n.rfind(L".organizador-log-", 0) == 0)
            g_logs.push_back({ n, e.path().wstring() });
    }
    std::sort(g_logs.begin(), g_logs.end(),
              [](const std::pair<std::wstring, std::wstring>& a,
                 const std::pair<std::wstring, std::wstring>& b) { return a.first > b.first; });
}

static INT_PTR CALLBACK DlgHistorial(HWND d, UINT m, WPARAM w, LPARAM) {
    switch (m) {
    case WM_INITDIALOG: {
        HWND lb = GetDlgItem(d, IDC_LISTA_LOG);
        for (const auto& x : g_logs) SendMessageW(lb, LB_ADDSTRING, 0, (LPARAM)x.first.c_str());
        if (!g_logs.empty()) SendMessageW(lb, LB_SETCURSEL, 0, 0);
        return TRUE;
    }
    case WM_COMMAND:
        if (LOWORD(w) == IDOK) {
            int sel = (int)SendMessageW(GetDlgItem(d, IDC_LISTA_LOG), LB_GETCURSEL, 0, 0);
            if (sel >= 0 && sel < (int)g_logs.size()) g_logElegido = g_logs[sel].second;
            EndDialog(d, IDOK);
            return TRUE;
        }
        if (LOWORD(w) == IDCANCEL) { EndDialog(d, IDCANCEL); return TRUE; }
        return FALSE;
    case WM_CLOSE: EndDialog(d, IDCANCEL); return TRUE;
    }
    return FALSE;
}

static void abrirHistorial() {
    recogerLogs();
    if (g_logs.empty()) { estado(L"No hay ninguna ejecución anterior en esta carpeta."); return; }
    HINSTANCE hi = (HINSTANCE)GetWindowLongPtrW(g_hwnd, GWLP_HINSTANCE);
    if (DialogBoxParamW(hi, MAKEINTRESOURCEW(IDD_HISTORIAL), g_hwnd, DlgHistorial, 0) != IDOK) return;
    if (g_logElegido.empty()) return;

    UndoResult r = deshacer(fs::path(g_logElegido), nullptr);
    std::wstring res = L"Deshecho: " + std::to_wstring(r.restored) + L" restaurados";
    if (r.skipped) res += L", " + std::to_wstring(r.skipped) + L" omitidos";
    if (r.errors)  res += L", " + std::to_wstring(r.errors) + L" con error";
    estado(res);
    g_logElegido.clear();
    simular();
}

// ------------------------------- reglas -------------------------------------

static std::wstring rutaReglas() { return carpetaDatos() + L"\\reglas.txt"; }

static void cargarReglas() {
    g_opt.customRules.clear();
    std::ifstream f(rutaReglas().c_str());
    if (!f) return;
    std::string l;
    while (std::getline(f, l)) {
        l = recortar(l);
        if (l.empty() || l[0] == '#' || l[0] == ';') continue;
        size_t p = l.find('=');
        if (p == std::string::npos || p == 0 || p + 1 >= l.size()) continue;
        std::string ext = l.substr(0, p), cat = l.substr(p + 1);
        if (ext[0] != '.') ext = "." + ext;
        g_opt.customRules.push_back({ toUtf8(lowerW(toWide(ext))), cat });
    }
}

static void guardarReglas(const std::vector<Rule>& rs) {
    std::ofstream f(rutaReglas().c_str(), std::ios::binary);
    if (!f) return;
    f << "# Reglas de FileNet: extension=carpeta (una por linea)\r\n";
    for (const auto& r : rs) f << r.ext << "=" << r.cat << "\r\n";
}

static std::vector<Rule> g_reglasEdit;

static void refrescarListaReglas(HWND d) {
    HWND lb = GetDlgItem(d, IDC_LISTA_REGLAS);
    SendMessageW(lb, LB_RESETCONTENT, 0, 0);
    for (const auto& r : g_reglasEdit) {
        std::wstring t = toWide(r.ext) + L"   ->   " + toWide(r.cat);
        SendMessageW(lb, LB_ADDSTRING, 0, (LPARAM)t.c_str());
    }
}

static INT_PTR CALLBACK DlgReglas(HWND d, UINT m, WPARAM w, LPARAM) {
    switch (m) {
    case WM_INITDIALOG:
        g_reglasEdit = g_opt.customRules;
        refrescarListaReglas(d);
        SetFocus(GetDlgItem(d, IDC_EXT));
        return FALSE;

    case WM_COMMAND:
        switch (LOWORD(w)) {
        case IDC_ANADIR: {
            wchar_t ext[64] = L"", cat[128] = L"";
            GetDlgItemTextW(d, IDC_EXT, ext, 64);
            GetDlgItemTextW(d, IDC_CAT, cat, 128);
            std::wstring e = ext, c = cat;
            while (!e.empty() && e.front() == L' ') e.erase(e.begin());
            while (!e.empty() && e.back() == L' ')  e.pop_back();
            while (!c.empty() && c.front() == L' ') c.erase(c.begin());
            while (!c.empty() && c.back() == L' ')  c.pop_back();
            if (e.empty() || c.empty()) {
                MessageBoxW(d, L"Escribe la extensión y la carpeta.", L"Reglas", MB_ICONINFORMATION);
                return TRUE;
            }
            if (e[0] != L'.') e = L"." + e;
            g_reglasEdit.push_back({ toUtf8(lowerW(e)), toUtf8(c) });
            SetDlgItemTextW(d, IDC_EXT, L"");
            SetDlgItemTextW(d, IDC_CAT, L"");
            refrescarListaReglas(d);
            SetFocus(GetDlgItem(d, IDC_EXT));
            return TRUE;
        }
        case IDC_QUITAR: {
            int sel = (int)SendMessageW(GetDlgItem(d, IDC_LISTA_REGLAS), LB_GETCURSEL, 0, 0);
            if (sel >= 0 && sel < (int)g_reglasEdit.size()) {
                g_reglasEdit.erase(g_reglasEdit.begin() + sel);
                refrescarListaReglas(d);
            }
            return TRUE;
        }
        case IDOK:
            guardarReglas(g_reglasEdit);
            g_opt.customRules = g_reglasEdit;
            EndDialog(d, IDOK);
            return TRUE;
        case IDCANCEL:
            EndDialog(d, IDCANCEL);
            return TRUE;
        }
        return FALSE;
    case WM_CLOSE: EndDialog(d, IDCANCEL); return TRUE;
    }
    return FALSE;
}

static void abrirReglas() {
    HINSTANCE hi = (HINSTANCE)GetWindowLongPtrW(g_hwnd, GWLP_HINSTANCE);
    if (DialogBoxParamW(hi, MAKEINTRESOURCEW(IDD_REGLAS), g_hwnd, DlgReglas, 0) == IDOK)
        simular();                       // recalcular con las reglas nuevas
}

// -------------------------- carpeta del movil -------------------------------

static std::string g_carpetaMovilElegida;
static std::wstring g_rutaInicialMovil;

static void listarCarpetasMovil(HWND d) {
    HWND lb = GetDlgItem(d, IDC_LISTA_CARP);
    SendMessageW(lb, LB_RESETCONTENT, 0, 0);

    wchar_t buf[512] = L"";
    GetDlgItemTextW(d, IDC_RUTA_MOVIL, buf, 512);
    if (buf[0] == 0) return;

    std::string salida = ejecutarAdb(L"-s " + toWide(g_movilId) + L" shell ls -p " + toWide(citaSH(toUtf8(buf))));
    for (const auto& l : lineas(salida)) {
        if (l.empty() || l.back() != '/') continue;
        std::string nombre = l.substr(0, l.size() - 1);
        if (nombre.empty()) continue;
        std::wstring w = toWide(nombre);
        SendMessageW(lb, LB_ADDSTRING, 0, (LPARAM)w.c_str());
    }
}

static INT_PTR CALLBACK DlgCarpetaMovil(HWND d, UINT m, WPARAM w, LPARAM) {
    switch (m) {
    case WM_INITDIALOG:
        SetDlgItemTextW(d, IDC_RUTA_MOVIL, g_rutaInicialMovil.c_str());
        listarCarpetasMovil(d);
        return TRUE;

    case WM_COMMAND:
        switch (LOWORD(w)) {
        case IDC_SUBIR: {
            wchar_t buf[512] = L"";
            GetDlgItemTextW(d, IDC_RUTA_MOVIL, buf, 512);
            std::string r = toUtf8(buf);
            while (r.size() > 1 && r.back() == '/') r.pop_back();
            size_t p = r.find_last_of('/');
            if (p != std::string::npos && p > 0) r = r.substr(0, p);
            SetDlgItemTextW(d, IDC_RUTA_MOVIL, toWide(r).c_str());
            listarCarpetasMovil(d);
            return TRUE;
        }
        case IDC_LISTA_CARP:
            if (HIWORD(w) == LBN_DBLCLK) {
                int sel = (int)SendMessageW(GetDlgItem(d, IDC_LISTA_CARP), LB_GETCURSEL, 0, 0);
                if (sel < 0) return TRUE;
                wchar_t nom[512] = L"";
                SendMessageW(GetDlgItem(d, IDC_LISTA_CARP), LB_GETTEXT, sel, (LPARAM)nom);
                wchar_t buf[512] = L"";
                GetDlgItemTextW(d, IDC_RUTA_MOVIL, buf, 512);
                std::string r = toUtf8(buf);
                while (!r.empty() && r.back() == '/') r.pop_back();
                r += "/" + toUtf8(nom);
                SetDlgItemTextW(d, IDC_RUTA_MOVIL, toWide(r).c_str());
                listarCarpetasMovil(d);
                return TRUE;
            }
            return FALSE;
        case IDOK: {
            wchar_t buf[512] = L"";
            GetDlgItemTextW(d, IDC_RUTA_MOVIL, buf, 512);
            g_carpetaMovilElegida = toUtf8(buf);
            EndDialog(d, IDOK);
            return TRUE;
        }
        case IDCANCEL: EndDialog(d, IDCANCEL); return TRUE;
        }
        return FALSE;
    case WM_CLOSE: EndDialog(d, IDCANCEL); return TRUE;
    }
    return FALSE;
}

static bool elegirCarpetaMovil(std::string& salida) {
    std::wstring ini = regLeeCadena(L"UltimaCarpetaMovil");
    g_rutaInicialMovil = ini.empty() ? L"/sdcard/Download" : ini;
    g_carpetaMovilElegida.clear();

    HINSTANCE hi = (HINSTANCE)GetWindowLongPtrW(g_hwnd, GWLP_HINSTANCE);
    if (DialogBoxParamW(hi, MAKEINTRESOURCEW(IDD_CARPETA_MOVIL), g_hwnd, DlgCarpetaMovil, 0) != IDOK)
        return false;
    if (g_carpetaMovilElegida.empty()) return false;

    salida = g_carpetaMovilElegida;
    regEscribeCadena(L"UltimaCarpetaMovil", toWide(salida));
    return true;
}

// ------------------------------ layout --------------------------------------

static void colocarControles(HWND h, int ancho, int alto) {
    const int M = 12, F1 = 30, BTN = 34, GAP = 8;
    int y = M;
    const int wTema = 130, wElegir = 150, wMovil = 90, wReglas = 90;

    int wRuta = ancho - (M + 70) - M - wElegir - wMovil - wReglas - wTema - 4 * GAP;
    if (wRuta < 120) wRuta = 120;
    int xTop = M + 70;
    MoveWindow(GetDlgItem(h, ID_EDIT_RUTA),  xTop, y, wRuta, F1, TRUE);
    xTop += wRuta + GAP;
    MoveWindow(GetDlgItem(h, ID_BTN_ELEGIR), xTop, y, wElegir, F1, TRUE);
    xTop += wElegir + GAP;
    MoveWindow(GetDlgItem(h, ID_BTN_MOVIL),  xTop, y, wMovil, F1, TRUE);
    xTop += wMovil + GAP;
    MoveWindow(GetDlgItem(h, ID_BTN_REGLAS), xTop, y, wReglas, F1, TRUE);
    MoveWindow(GetDlgItem(h, ID_BTN_TEMA),   ancho - M - wTema, y, wTema, F1, TRUE);
    y += F1 + GAP;

    MoveWindow(GetDlgItem(h, ID_CHK_FECHA), M, y, 260, 22, TRUE);
    MoveWindow(GetDlgItem(h, ID_CHK_RECUR), M + 270, y, 260, 22, TRUE);
    y += 30 + GAP;

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
        g_hwnd = h;                       // durante WM_CREATE aun no esta asignado
        HINSTANCE hi = (HINSTANCE)GetWindowLongPtrW(h, GWLP_HINSTANCE);

        CreateWindowExW(0, L"STATIC", L"Carpeta:", WS_CHILD | WS_VISIBLE | SS_LEFT,
                       0, 0, 60, 20, h, nullptr, hi, nullptr);
        CreateWindowExW(0, L"EDIT", L"", WS_CHILD | WS_VISIBLE | WS_BORDER | ES_AUTOHSCROLL | ES_READONLY,
                       0, 0, 100, 24, h, (HMENU)ID_EDIT_RUTA, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Elegir carpeta…", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 24, h, (HMENU)ID_BTN_ELEGIR, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Tema: claro", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 24, h, (HMENU)ID_BTN_TEMA, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Móvil", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 24, h, (HMENU)ID_BTN_MOVIL, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Reglas…", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 24, h, (HMENU)ID_BTN_REGLAS, hi, nullptr);

        CreateWindowExW(0, L"BUTTON", L"Agrupar por fecha (Año\\Mes)", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 22, h, (HMENU)ID_CHK_FECHA, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Incluir subcarpetas", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 22, h, (HMENU)ID_CHK_RECUR, hi, nullptr);

        CreateWindowExW(WS_EX_CLIENTEDGE, WC_LISTVIEWW, L"",
                       WS_CHILD | WS_VISIBLE | LVS_REPORT | LVS_SINGLESEL | LVS_SHOWSELALWAYS | WS_BORDER,
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
        // anchos definitivos (una sola vez; cambiarlos reordena TODAS las filas)
        for (int c = 0; c < 5; ++c)
            ListView_SetColumnWidth(hl, c, c == 3 ? 260 : (c == 0 ? 240 : (c == 4 ? 90 : 110)));
        if (!g_imgSmall) {
            SHFILEINFOW sfi{};
            g_imgSmall = (HIMAGELIST)SHGetFileInfoW(L"C:\\", FILE_ATTRIBUTE_DIRECTORY, &sfi, sizeof(sfi),
                                                    SHGFI_SYSICONINDEX | SHGFI_SMALLICON);
            if (g_imgSmall) SendMessageW(hl, LVM_SETIMAGELIST, LVSIL_SMALL, (LPARAM)g_imgSmall);
        }

        CreateWindowExW(0, L"BUTTON", L"Simular", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_SIMULAR, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Aplicar", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_APLICAR, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Deshacer", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_DESHACER, hi, nullptr);
        CreateWindowExW(0, L"BUTTON", L"Abrir carpeta", WS_CHILD | WS_VISIBLE | BS_OWNERDRAW,
                       0, 0, 100, 28, h, (HMENU)ID_BTN_ABRIR, hi, nullptr);
        CreateWindowExW(0, L"STATIC", L"", WS_CHILD | WS_VISIBLE | SS_OWNERDRAW,
                       0, 0, 100, 22, h, (HMENU)ID_PROGRESO, hi, nullptr);
        CreateWindowExW(0, L"STATIC", L"", WS_CHILD | WS_VISIBLE | SS_LEFT,
                       0, 0, 100, 20, h, (HMENU)ID_ESTADO, hi, nullptr);

        NONCLIENTMETRICSW ncm{};
        ncm.cbSize = sizeof(ncm);
        if (SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof(ncm), &ncm, 0))
            g_font = CreateFontIndirectW(&ncm.lfMessageFont);
        else
            g_font = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
        for (HWND hijo = GetWindow(h, GW_CHILD); hijo; hijo = GetWindow(hijo, GW_HWNDNEXT)) setFont(hijo, g_font);

        EnableWindow(GetDlgItem(h, ID_BTN_APLICAR), FALSE);
        EnableWindow(GetDlgItem(h, ID_BTN_DESHACER), FALSE);

        DragAcceptFiles(h, TRUE);
        aplicarTema();
        return 0;
    }

    case WM_SIZE:
        colocarControles(h, LOWORD(l), HIWORD(l));
        return 0;

    case WM_ERASEBKGND: {
        RECT r;
        GetClientRect(h, &r);
        FillRect((HDC)w, &r, g_brFondo ? g_brFondo : (HBRUSH)(COLOR_BTNFACE + 1));
        return 1;
    }

    case WM_CTLCOLORSTATIC:
    case WM_CTLCOLOREDIT: {
        HDC dc = (HDC)w;
        HWND ctl = (HWND)l;
        bool esRuta = (ctl == GetDlgItem(h, ID_EDIT_RUTA));
        SetTextColor(dc, esRuta ? PAL->texto : PAL->texto);
        SetBkColor(dc, esRuta ? PAL->panel : PAL->fondo);
        return (LRESULT)(esRuta ? g_brPanel : g_brFondo);
    }

    case WM_DRAWITEM: {
        LPDRAWITEMSTRUCT di = (LPDRAWITEMSTRUCT)l;
        switch (di->CtlID) {
        case ID_CHK_FECHA: pintarCheck(di, g_opt.byDate);  return TRUE;
        case ID_CHK_RECUR: pintarCheck(di, g_opt.recursive); return TRUE;
        case ID_PROGRESO:  pintarProgreso(di); return TRUE;
        default:           pintarBoton(di); return TRUE;
        }
    }

    case WM_MOUSEMOVE: {
        POINT pt{ (short)LOWORD(l), (short)HIWORD(l) };
        HWND bajo = ChildWindowFromPointEx(h, pt, CWP_SKIPINVISIBLE | CWP_SKIPTRANSPARENT);
        int id = bajo ? GetDlgCtrlID(bajo) : 0;
        if (id != g_hover) {
            if (g_hover) InvalidateRect(GetDlgItem(h, g_hover), nullptr, TRUE);
            g_hover = id;
            if (g_hover) InvalidateRect(GetDlgItem(h, g_hover), nullptr, TRUE);
        }
        return 0;
    }

    case WM_COMMAND:
        switch (LOWORD(w)) {
        case ID_BTN_ELEGIR: {
            fs::path p;
            if (elegirCarpeta(h, p)) fijarCarpeta(p);
            return 0;
        }
        case ID_BTN_TEMA:
            g_tema = g_tema ? 0 : 1;
            guardarTema();
            aplicarTema();
            return 0;
        case ID_BTN_SIMULAR: simular(); return 0;
        case ID_BTN_MOVIL:   verMovil(); return 0;
        case ID_BTN_REGLAS:  abrirReglas(); return 0;
        case ID_BTN_APLICAR: aplicarTodo(); return 0;
        case ID_BTN_DESHACER:
            if (g_ultimoFueMovil) deshacerMovil();
            else                  abrirHistorial();
            return 0;
        case ID_BTN_ABRIR: abrirCarpeta(); return 0;
        case ID_CHK_FECHA:
            g_opt.byDate = !g_opt.byDate;
            InvalidateRect(GetDlgItem(h, ID_CHK_FECHA), nullptr, TRUE);
            simular();
            return 0;
        case ID_CHK_RECUR:
            g_opt.recursive = !g_opt.recursive;
            InvalidateRect(GetDlgItem(h, ID_CHK_RECUR), nullptr, TRUE);
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
                if (st == 1) cd->clrText = PAL->ok;
                else if (st == 2) cd->clrText = PAL->error;
                else cd->clrText = PAL->texto;
                cd->clrTextBk = PAL->panel;
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
        liberarPinceles();
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

    cargarTema();
    cargarReglas();

    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = hi;
    wc.lpszClassName = L"FileNetGUI";
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = nullptr;               // lo pintamos en WM_ERASEBKGND
    HICON icoGrande = (HICON)LoadImageW(hi, MAKEINTRESOURCEW(1), IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_SHARED);
    HICON icoChico  = (HICON)LoadImageW(hi, MAKEINTRESOURCEW(1), IMAGE_ICON, 16, 16, LR_SHARED);
    wc.hIcon   = icoGrande ? icoGrande : LoadIcon(nullptr, IDI_APPLICATION);
    wc.hIconSm = icoChico  ? icoChico  : wc.hIcon;
    RegisterClassExW(&wc);

    g_hwnd = CreateWindowExW(0, L"FileNetGUI", L"FileNet · Organizador de carpetas",
                             WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 1020, 660,
                             nullptr, nullptr, hi, nullptr);
    if (!g_hwnd) return 1;

    ShowWindow(g_hwnd, SW_SHOW);
    UpdateWindow(g_hwnd);

    // carpeta inicial: la del argumento, o la ultima usada, o Descargas
    {
        std::error_code ec;
        fs::path inicial;
        auto args = getWideArgs();
        if (args.size() > 1) {
            fs::path p(args[1]);
            if (fs::is_directory(p, ec)) inicial = p;
        }
        if (inicial.empty()) {
            std::wstring ult = regLeeCadena(L"UltimaCarpeta");
            if (!ult.empty() && fs::is_directory(fs::path(ult), ec)) inicial = fs::path(ult);
        }
        if (inicial.empty()) inicial = defaultTargetDir();
        fijarCarpeta(inicial);
    }

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
