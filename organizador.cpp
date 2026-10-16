// =============================================================================
//  organizador.cpp  --  Organizador de carpetas (version CONSOLA)
//
//  La logica vive en nucleo.hpp (compartida con la version con ventana).
//  Compilar:
//    g++ -std=c++20 -O2 -Wall -Wextra -static -o organizador.exe organizador.cpp -lshell32
// =============================================================================

#include "nucleo.hpp"
#include "version.hpp"

#include <iostream>
#include <string>
#include <vector>

using namespace org;

// ============================ salida de consola =============================

static bool g_color = false;
static bool g_ansi  = false;

static const char* RESET = "\x1b[0m";
static const char* BOLD  = "\x1b[1m";
static const char* GREY  = "\x1b[90m";

static bool enableAnsi() {
    HANDLE h = GetStdHandle(STD_OUTPUT_HANDLE);
    if (h == INVALID_HANDLE_VALUE || h == nullptr) return false;
    DWORD mode = 0;
    if (!GetConsoleMode(h, &mode)) return false;
    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT;
    return SetConsoleMode(h, mode) != 0;
}

static std::string c(const std::string& code, const std::string& s) {
    if (!g_color) return s;
    return code + s + RESET;
}

static void out(const std::string& s) { std::cout << s; }
static void line(const std::string& s = "") { std::cout << s << '\n'; }

static int dispWidth(const std::string& s) {
    std::wstring w = toWide(s);
    int width = 0;
    for (size_t i = 0; i < w.size(); ++i) {
        wchar_t ch = w[i];
        if (ch >= 0xD800 && ch <= 0xDBFF) { width += 2; ++i; continue; }
        if (ch >= 0x1100 && (ch <= 0x115F || ch == 0x2329 || ch == 0x232A)) { width += 2; continue; }
        width += 1;
    }
    return width;
}

static std::string repeat(const std::string& t, int n) { std::string r; for (int i = 0; i < n; ++i) r += t; return r; }
static std::string padRight(const std::string& s, int w) { int d = dispWidth(s); return d >= w ? s : s + repeat(" ", w - d); }
static std::string padLeft(const std::string& s, int w)  { int d = dispWidth(s); return d >= w ? s : repeat(" ", w - d) + s; }

static const int BOXW = 62;

static void banner() {
    const char* border = "\x1b[38;5;39m";
    std::string title = T8("  🗂  ORGANIZADOR DE CARPETAS", "  🗂  FOLDER ORGANIZER");
    std::string ver   = "v" + toUtf8(std::wstring(FILENEST_VERSION));
    int pad = BOXW - dispWidth(title) - dispWidth(ver) - 1;
    if (pad < 1) pad = 1;
    std::string content = title + repeat(" ", pad) + ver + " ";
    if (dispWidth(content) < BOXW) content += repeat(" ", BOXW - dispWidth(content));
    line();
    line("  " + c(border, std::string("╔") + repeat("═", BOXW) + "╗"));
    line("  " + c(border, "║") + c(BOLD, content) + c(border, "║"));
    line("  " + c(border, std::string("╚") + repeat("═", BOXW) + "╝"));
    line();
}

static void printMenu() {
    banner();
    line("   " + c("\x1b[96m", "[1]") + T8("  Elegir una carpeta y organizarla   ", "  Choose a folder and organize it   ") + c(GREY, T8("(simula y luego pregunta)", "(simulates, then asks)")));
    line("   " + c("\x1b[96m", "[2]") + T8("  Simular sobre mi carpeta Descargas", "  Simulate on my Downloads folder"));
    line("   " + c("\x1b[96m", "[3]") + T8("  Aplicar en mi carpeta Descargas", "  Apply to my Downloads folder"));
    line("   " + c("\x1b[96m", "[4]") + T8("  Deshacer la ultima ejecucion de Descargas", "  Undo the last run on Downloads"));
    line("   " + c("\x1b[96m", "[5]") + T8("  Deshacer indicando la carpeta", "  Undo specifying the folder"));
    line("   " + c(GREY,   T8("[0]  Salir", "[0]  Exit")));
    line();
    line("   " + c(GREY, T8("Consejo: también hay una versión con ventana: ", "Tip: there is also a window version: ")) + c(BOLD, "Organizador de carpetas.exe"));
    line();
}

static void printHelp() {
    banner();
    line("  " + c(BOLD, T8("USO", "USAGE")));
    line(T8("    organizador.exe [opciones]", "    organizador.exe [options]"));
    line();
    line("  " + c(BOLD, T8("OPCIONES", "OPTIONS")));
    line(T8("    --dir <ruta>        Carpeta a organizar (por defecto: tu carpeta Descargas)", "    --dir <path>        Folder to organize (default: your Downloads folder)"));
    line(T8("    --apply             Aplica los cambios. Sin esto solo SIMULA (no toca nada)", "    --apply             Applies the changes. Without it, it only SIMULATES (nothing is touched)"));
    line(T8("    --by-date           Dentro de cada categoria crea subcarpetas Anio\\Mes", "    --by-date           Inside each category, creates Year\\Month subfolders"));
    line(T8("    --recursive, -r     Incluye tambien los archivos de las subcarpetas", "    --recursive, -r     Also includes files inside subfolders"));
    line(T8("    --rule .ext=Carpeta Anade o modifica una regla (ej: --rule .psd=Diseno)", "    --rule .ext=Folder  Adds or changes a rule (e.g. --rule .psd=Design)"));
    line(T8("    --undo <log.csv>    Deshace una ejecucion usando su archivo de registro", "    --undo <log.csv>    Undoes a run using its log file"));
    line(T8("    --color / --no-color   Fuerza o desactiva los colores", "    --color / --no-color   Forces or disables colors"));
    line(T8("    --quiet, -q         Menos salida por pantalla", "    --quiet, -q         Less output on screen"));
    line(T8("    --menu              Muestra el menu principal (lo usa Organizador.bat)", "    --menu              Shows the main menu (used by Organizador.bat)"));
    line(T8("    --help, -h          Muestra esta ayuda", "    --help, -h          Shows this help"));
    line();
    line("  " + c(BOLD, T8("CATEGORIAS POR DEFECTO", "DEFAULT CATEGORIES")));
    for (const auto& d : defaultCats()) {
        std::string exts;
        for (size_t i = 0; i < d.exts.size(); ++i) { if (i) exts += ", "; exts += d.exts[i]; }
        line("    " + iconOf(d.name) + " " + c(d.color, padRight(d.name, 14)) + c(GREY, exts));
    }
    line("    " + iconOf(kOther()) + " " + c(GREY, padRight(kOther(), 14)) + c(GREY, T8("el resto de extensiones", "everything else")));
    line();
}

// ============================ argumentos ====================================

struct Args {
    Options opt;
    bool apply = false, quiet = false, undo = false, help = false, menu = false, lang = false;
    int  colorMode = 0;      // 0 auto, 1 forzar, -1 desactivar
    fs::path undoLog;
};

static bool parseArgs(const std::vector<std::wstring>& a, Args& A) {
    A.opt.dir = defaultTargetDir();
    for (size_t i = 1; i < a.size(); ++i) {
        const std::wstring& s = a[i];
        auto needValue = [&](const wchar_t* name) -> const std::wstring* {
            if (i + 1 >= a.size()) { line("  " + c("\x1b[91m", std::string(T8("ERROR: falta el valor de ", "ERROR: missing value for ")) + toUtf8(name))); return nullptr; }
            return &a[++i];
        };
        if (s == L"--help" || s == L"-h" || s == L"/?") { A.help = true; }
        else if (s == L"--menu")      { A.menu = true; }
        else if (s == L"--lang")      { A.lang = true; }   // uso interno (lo llama Organizador.bat)
        else if (s == L"--apply")     { A.apply = true; }
        else if (s == L"--by-date")   { A.opt.byDate = true; }
        else if (s == L"--recursive" || s == L"-r") { A.opt.recursive = true; }
        else if (s == L"--quiet" || s == L"-q")     { A.quiet = true; }
        else if (s == L"--color")     { A.colorMode = 1; }
        else if (s == L"--no-color")  { A.colorMode = -1; }
        else if (s == L"--dir" || s == L"-d") {
            const std::wstring* v = needValue(L"--dir");
            if (!v) return false;
            A.opt.dir = fs::path(*v);
        }
        else if (s == L"--undo") {
            const std::wstring* v = needValue(L"--undo");
            if (!v) return false;
            A.undo = true;
            A.undoLog = fs::path(*v);
        }
        else if (s == L"--rule") {
            const std::wstring* v = needValue(L"--rule");
            if (!v) return false;
            size_t eq = v->find(L'=');
            if (eq == std::wstring::npos || eq == 0 || eq + 1 >= v->size()) {
                line("  " + c("\x1b[91m", T8("ERROR: --rule debe tener la forma .ext=Carpeta (ej: --rule .psd=Diseno)", "ERROR: --rule must look like .ext=Folder (e.g. --rule .psd=Design)")));
                return false;
            }
            std::wstring ext = lowerW(v->substr(0, eq));
            std::wstring cat = v->substr(eq + 1);
            if (ext[0] != L'.') ext = L"." + ext;
            A.opt.customRules.push_back({ toUtf8(ext), toUtf8(cat) });
        }
        else {
            line("  " + c("\x1b[91m", T8("ERROR: opcion desconocida: ", "ERROR: unknown option: ") + toUtf8(s)));
            line(T8("  Usa --help para ver las opciones disponibles.", "  Use --help to see the available options."));
            return false;
        }
    }
    return true;
}

// ============================ presentacion ==================================

static void kv(const std::string& icon, const std::string& key, const std::string& value) {
    line("   " + icon + " " + c(GREY, padRight(key, 13)) + " " + value);
}

static void drawBarChart(const std::map<std::string, long long>& cuenta) {
    long long maxC = 1;
    int maxName = 5;
    for (const auto& kvp : cuenta) { maxC = std::max(maxC, kvp.second); maxName = std::max(maxName, dispWidth(kvp.first)); }
    line("   " + c(GREY, T8("CATEGORÍAS", "CATEGORIES")));
    for (const auto& kvp : cuenta) {
        int filled = (int)((kvp.second * 20) / maxC);
        if (filled == 0 && kvp.second > 0) filled = 1;
        std::string bar;
        for (int i = 0; i < 20; ++i) bar += (i < filled ? "█" : "░");
        line("   " + iconOf(kvp.first) + " " + c(colorOf(kvp.first), padRight(kvp.first, maxName)) + "  " +
             c(BOLD, padLeft(std::to_string(kvp.second), 3)) + "  " + c(colorOf(kvp.first), bar));
    }
    line();
}

static void drawFileList(const std::vector<Move>& moves, const fs::path& base, int limite) {
    int maxName = 0;
    size_t n = std::min((size_t)limite, moves.size());
    for (size_t i = 0; i < n; ++i) maxName = std::max(maxName, std::min(dispWidth(toUtf8(moves[i].from.filename().wstring())), 32));
    for (size_t i = 0; i < n; ++i) {
        const Move& m = moves[i];
        std::string fn = toUtf8(m.from.filename().wstring());
        std::string shown = fn;
        if (dispWidth(shown) > 32) {
            std::wstring w = toWide(shown);
            while (!w.empty() && dispWidth(toUtf8(w)) > 29) w.pop_back();
            shown = toUtf8(w) + "...";
        }
        // lexically_relative: no toca el disco (fs::relative si, y era muy lento)
        fs::path rel = m.to.lexically_relative(base);
        std::string relStr = rel.empty() ? toUtf8(m.to.wstring()) : toUtf8(rel.wstring());
        line("   " + iconOf(m.cat) + " " + padRight(shown, maxName) + c(GREY, "  →  ") + c(colorOf(m.cat), relStr));
    }
    if (moves.size() > n) line("   " + c(GREY, T8("   … y ", "   … and ") + std::to_string((long long)(moves.size() - n)) + T8(" archivo(s) más", " more file(s)")));
    line();
}

static void progressBar(long long hecho, long long total) {
    if (!g_ansi) return;
    const int WPID = 28;
    int filled = total ? (int)((hecho * WPID) / total) : WPID;
    std::string bar;
    for (int i = 0; i < WPID; ++i) bar += (i < filled ? "█" : "░");
    int pct = total ? (int)((hecho * 100) / total) : 100;
    out("\r   " + c(std::string("\x1b[94m"), bar) + "  " + c(BOLD, padLeft(std::to_string(pct), 3) + "%") +
        "  " + c(GREY, std::to_string(hecho) + "/" + std::to_string(total)) + "\x1b[K");
    std::cout.flush();
}

// ============================ modos =========================================

static int runOrganize(Args& A) {
    std::error_code ec;
    if (!fs::exists(A.opt.dir, ec) || !fs::is_directory(A.opt.dir, ec)) {
        banner();
        line("  " + c("\x1b[91m", T8("✖ ERROR: la carpeta no existe o no es una carpeta:", "✖ ERROR: the folder does not exist or is not a folder:")));
        line("      " + toUtf8(A.opt.dir.wstring()));
        line();
        return 2;
    }
    fs::path dir = fs::absolute(A.opt.dir, ec);
    if (ec) dir = A.opt.dir;

    banner();
    kv("📁", T8("Carpeta", "Folder"), c(BOLD, toUtf8(dir.wstring())));
    kv("⚙",  T8("Modo", "Mode"), A.apply ? c("\x1b[93m", T8("APLICAR", "APPLY")) + c(GREY, T8("  ·  se moverán archivos", "  ·  files will be moved"))
                             : c("\x1b[96m", T8("SIMULACIÓN", "SIMULATION")) + c(GREY, T8("  ·  no se moverá nada", "  ·  nothing will be moved")));
    kv("📅", T8("Fecha", "Date"), A.opt.byDate ? T8("sí", "yes") + c(GREY, T8("  ·  agrupar por Año\\Mes", "  ·  group by Year\\Month")) : c(GREY, T8("no", "no")));
    kv("🔁", T8("Subcarpetas", "Subfolders"), A.opt.recursive ? T8("sí", "yes") + c(GREY, T8("  ·  recursivo", "  ·  recursive")) : c(GREY, T8("no", "no")));
    line();

    Plan plan = planificar(dir, A.opt);

    std::string stats = std::string("   ") + c(BOLD, std::to_string(plan.analyzed)) + T8(" analizados", " analyzed") +
                        c(GREY, "   ·   ") + c(BOLD, std::to_string((long long)plan.moves.size())) + T8(" a clasificar", " to sort");
    if (plan.skipped) stats += c(GREY, "   ·   ") + std::to_string(plan.skipped) + T8(" omitidos", " skipped");
    line(stats);
    line();

    if (plan.moves.empty()) {
        line("  " + c("\x1b[92m", T8("✔ Nada que organizar: la carpeta ya está en orden.", "✔ Nothing to organize: the folder is already tidy.")));
        line();
        return 0;
    }

    if (!A.quiet) {
        drawBarChart(plan.porCategoria);
        if (!A.apply) {
            line("   " + c(GREY, T8("ARCHIVOS", "FILES")));
            drawFileList(plan.moves, dir, 40);
        }
    }

    if (!A.apply) {
        line("  " + c("\x1b[93m", T8("⚠ SIMULACIÓN", "⚠ SIMULATION")) + c(GREY, T8(": no se movió nada.", ": nothing was moved.")));
        line("   " + c(GREY, T8("Para aplicarlo: ", "To apply it: ")) + c(BOLD, "--apply") + c(GREY, T8("  ·  o abriendo ", "  ·  or by opening ")) + c(BOLD, "\"Organizador.bat\""));
        line();
        return 0;
    }

    long long total = (long long)plan.moves.size();
    ApplyResult r = aplicar(plan, dir, [&](const Move& m, bool ok, const std::string& err) {
        if (!ok) {
            if (g_ansi) out("\x1b[K");
            line("   " + c("\x1b[91m", "✖ ") + toUtf8(m.from.filename().wstring()) + c(GREY, ": " + err));
        }
        progressBar(r.moved + r.errors + 1, total);
    });
    if (g_ansi) { out("\x1b[K"); line(); }
    line();

    line("  " + c("\x1b[92m", repeat("─", BOXW + 2)));
    line("  " + c("\x1b[92m", T8("✔ COMPLETADO", "✔ COMPLETED")) + "    " + c(GREY, T8("Movidos: ", "Moved: ")) + c(BOLD, std::to_string(r.moved)) +
         "    " + c(GREY, T8("Errores: ", "Errors: ")) + (r.errors ? c("\x1b[91m", std::to_string(r.errors)) : c(BOLD, "0")));
    line("  " + c(GREY, T8("Registro: ", "Log: ")) + r.logName);
    line("  " + c("\x1b[92m", repeat("─", BOXW + 2)));
    line();
    line("   " + c(GREY, T8("Para deshacer: ", "To undo: ")) + c(BOLD, "\"Organizador.bat\"") +
         c(GREY, T8("  ·  o bien: ", "  ·  or else: ")) + c(BOLD, "organizador.exe --undo \"" + toUtf8(r.log.wstring()) + "\""));
    line();
    return r.errors ? 1 : 0;
}

static int runUndo(const Args& A) {
    banner();
    std::error_code ec;
    if (!fs::exists(A.undoLog, ec)) {
        line("  " + c("\x1b[91m", T8("✖ ERROR: no encuentro el archivo de registro:", "✖ ERROR: log file not found:")));
        line("      " + toUtf8(A.undoLog.wstring()));
        line();
        return 2;
    }
    UndoResult r = deshacer(A.undoLog, [&](const fs::path& p, bool ok) {
        if (!ok) line("   " + c("\x1b[91m", "✖ ") + toUtf8(p.filename().wstring()));
    });
    line("  " + c("\x1b[92m", repeat("─", BOXW + 2)));
    line("  " + c("\x1b[92m", T8("✔ DESHACER COMPLETADO", "✔ UNDO COMPLETED")) + "    " + c(GREY, T8("Restaurados: ", "Restored: ")) + c(BOLD, std::to_string(r.restored)) +
         (r.skipped ? c(GREY, T8("    Omitidos: ", "    Skipped: ")) + std::to_string(r.skipped) : "") +
         (r.errors ? c(GREY, T8("    Errores: ", "    Errors: ")) + c("\x1b[91m", std::to_string(r.errors)) : ""));
    line("  " + c("\x1b[92m", repeat("─", BOXW + 2)));
    line();
    return r.errors ? 1 : 0;
}

// ============================ main ==========================================

int main() {
    SetConsoleOutputCP(CP_UTF8);
    g_ansi = enableAnsi();

    Args A;
    if (!parseArgs(getWideArgs(), A)) return 2;

    {
        wchar_t buf[8];
        bool noColorEnv = (GetEnvironmentVariableW(L"NO_COLOR", buf, 8) > 0);
        if (A.colorMode == 1)       g_color = true;
        else if (A.colorMode == -1) g_color = false;
        else                        g_color = g_ansi && !noColorEnv;
    }

    if (A.lang) { printf("%s\n", filenestEspanol() ? "es" : "en"); return 0; }
    if (A.help) { printHelp(); return 0; }
    if (A.menu) { printMenu(); return 0; }
    if (A.undo) return runUndo(A);
    return runOrganize(A);
}
