// =============================================================================
//  nucleo.hpp  --  Motor compartido del Organizador de carpetas
//
//  Contiene TODA la logica (categorias, escaneo, planificacion, aplicar y
//  deshacer), sin nada de presentacion. Lo usan las dos versiones:
//     - organizador.cpp  (consola)
//     - gui.cpp          (ventana Windows)
// =============================================================================
#pragma once

#include <windows.h>
#include <shellapi.h>

#include <algorithm>
#include <cstdint>
#include <cstdio>
#include <cwchar>
#include <cwctype>
#include <filesystem>
#include <fstream>
#include <functional>
#include <map>
#include <set>
#include <sstream>
#include <string>
#include <vector>

namespace org {

namespace fs = std::filesystem;

// ============================ texto =========================================

inline std::string toUtf8(const std::wstring& w) {
    if (w.empty()) return {};
    int n = WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), nullptr, 0, nullptr, nullptr);
    if (n <= 0) return {};
    std::string s((size_t)n, '\0');
    WideCharToMultiByte(CP_UTF8, 0, w.c_str(), (int)w.size(), s.data(), n, nullptr, nullptr);
    return s;
}

inline std::wstring toWide(const std::string& s) {
    if (s.empty()) return {};
    int n = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), nullptr, 0);
    if (n <= 0) return {};
    std::wstring w((size_t)n, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, s.c_str(), (int)s.size(), w.data(), n);
    return w;
}

inline std::wstring lowerW(std::wstring s) { for (auto& c : s) c = (wchar_t)std::towlower(c); return s; }

inline std::vector<std::wstring> getWideArgs() {
    std::vector<std::wstring> args;
    int argc = 0;
    LPWSTR* argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    if (!argv) return args;
    for (int i = 0; i < argc; ++i) args.emplace_back(argv[i]);
    LocalFree(argv);
    return args;
}

inline std::string nowStamp() {
    SYSTEMTIME st{};
    GetLocalTime(&st);
    char buf[32];
    snprintf(buf, 32, "%04u%02u%02u-%02u%02u%02u", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond);
    return buf;
}

// ============================ categorias ====================================

struct CatDef { const char* name; const char* icon; const char* color; std::vector<const char*> exts; };

inline const std::vector<CatDef>& defaultCats() {
    static const std::vector<CatDef> cats = {
        { "Imágenes",    "📷", "\x1b[96m",       { "jpg","jpeg","png","gif","bmp","webp","tif","tiff","svg","ico","heic","raw","cr2","nef","psd","avif" } },
        { "Documentos",  "📄", "\x1b[94m",       { "pdf","doc","docx","txt","rtf","odt","xls","xlsx","ods","ppt","pptx","odp","csv","md","epub" } },
        { "Vídeos",      "🎬", "\x1b[95m",       { "mp4","mkv","avi","mov","wmv","flv","webm","m4v","mpg","mpeg","3gp","ts" } },
        { "Audio",       "🎵", "\x1b[92m",       { "mp3","wav","flac","aac","ogg","wma","m4a","opus","mid","midi" } },
        { "Comprimidos", "📦", "\x1b[93m",       { "zip","rar","7z","tar","gz","bz2","xz","iso","cab","zst" } },
        { "Programas",   "🔧", "\x1b[91m",       { "exe","msi","msix","appx","bat","cmd","com","apk","deb","rpm" } },
        { "Código",      "💻", "\x1b[38;5;208m", { "c","cpp","cc","h","hpp","cs","java","py","js","ts","html","htm","css","php","rs","go","rb","sh","json","xml","yml","yaml","sql","ini","toml" } },
        { "Fuentes",     "🔠", "\x1b[38;5;213m", { "ttf","otf","woff","woff2" } },
    };
    return cats;
}

inline const char* kOther() { return "Otros"; }

inline std::string iconOf(const std::string& cat) {
    if (cat == kOther()) return "📎";
    for (const auto& d : defaultCats()) if (cat == d.name) return d.icon;
    return "🏷";
}

inline std::string colorOf(const std::string& cat) {
    if (cat == kOther()) return "\x1b[90m";
    for (const auto& d : defaultCats()) if (cat == d.name) return d.color;
    return "\x1b[36m";
}

// ============================ opciones / plan ===============================

struct Rule { std::string ext; std::string cat; };

struct Options {
    fs::path dir;
    bool byDate = false;
    bool recursive = false;
    std::vector<Rule> customRules;
};

struct Move { fs::path from; fs::path to; std::string cat; };

struct Plan {
    std::vector<Move> moves;
    long long analyzed = 0;
    long long skipped = 0;
    std::map<std::string, long long> porCategoria;
};

struct Context {
    const Options* opt = nullptr;
    std::map<std::string, std::string> extToCat;
    std::set<std::string> catNamesLower;
    std::vector<Move> moves;
    std::set<std::string> reserved;
    long long analyzed = 0;
    long long skipped = 0;
};

inline fs::path defaultTargetDir() {
    wchar_t buf[32767];
    DWORD n = GetEnvironmentVariableW(L"USERPROFILE", buf, 32767);
    fs::path home = (n > 0 && n < 32767) ? fs::path(buf) : fs::path(L"C:\\");
    fs::path dl = home / L"Downloads";
    if (fs::exists(dl)) return dl;
    return home;
}

// ============================ reglas de exclusion ===========================

inline bool isHiddenOrSystem(const fs::path& p) {
    DWORD attrs = GetFileAttributesW(p.c_str());
    if (attrs == INVALID_FILE_ATTRIBUTES) return true;
    if (attrs & FILE_ATTRIBUTE_HIDDEN) return true;
    if (attrs & FILE_ATTRIBUTE_SYSTEM) return true;
    if (attrs & FILE_ATTRIBUTE_REPARSE_POINT) return true;
    return false;
}

inline bool isSkippedName(const std::wstring& name) {
    std::wstring low = lowerW(name);
    if (low.empty() || low[0] == L'.') return true;
    if (low == L"desktop.ini") return true;
    if (low == L"thumbs.db") return true;
    return false;
}

inline bool lastWriteParts(const fs::path& p, WORD& year, WORD& month) {
    WIN32_FILE_ATTRIBUTE_DATA fad{};
    if (!GetFileAttributesExW(p.c_str(), GetFileExInfoStandard, &fad)) return false;
    FILETIME local{};
    if (!FileTimeToLocalFileTime(&fad.ftLastWriteTime, &local)) return false;
    SYSTEMTIME st{};
    if (!FileTimeToSystemTime(&local, &st)) return false;
    year = st.wYear; month = st.wMonth;
    return true;
}

// ============================ planificacion =================================

inline std::string categoryOf(const Context& ctx, const fs::path& p) {
    std::string ext = toUtf8(lowerW(p.extension().wstring()));
    auto it = ctx.extToCat.find(ext);
    return (it == ctx.extToCat.end()) ? std::string(kOther()) : it->second;
}

inline std::string keyOf(const fs::path& p) { return toUtf8(lowerW(p.wstring())); }

inline fs::path reserveUnique(Context& ctx, fs::path dest) {
    auto libre = [&](const fs::path& p) {
        if (ctx.reserved.count(keyOf(p))) return false;
        std::error_code ec;
        if (fs::exists(p, ec)) return false;
        return true;
    };
    if (libre(dest)) { ctx.reserved.insert(keyOf(dest)); return dest; }
    fs::path dir = dest.parent_path();
    std::wstring stem = dest.stem().wstring();
    std::wstring ext = dest.extension().wstring();
    for (int i = 1; i < 1000000; ++i) {
        fs::path cand = dir / (stem + L" (" + std::to_wstring(i) + L")" + ext);
        if (libre(cand)) { ctx.reserved.insert(keyOf(cand)); return cand; }
    }
    ctx.reserved.insert(keyOf(dest));
    return dest;
}

inline void collectDir(Context& ctx, const fs::path& dir, int depth) {
    if (depth > 64) return;
    std::error_code ec;
    fs::directory_iterator it(dir, fs::directory_options::skip_permission_denied, ec);
    if (ec) return;

    for (const fs::directory_entry& entry : it) {
        const fs::path& p = entry.path();
        std::error_code sec;
        bool esDir = entry.is_directory(sec);
        if (sec) continue;

        if (esDir) {
            if (ctx.opt->recursive) {
                std::wstring name = lowerW(p.filename().wstring());
                std::string n8 = toUtf8(name);
                if (ctx.catNamesLower.count(n8)) continue;
                if (name.empty() || name[0] == L'.') continue;
                collectDir(ctx, p, depth + 1);
            }
            continue;
        }

        std::wstring name = p.filename().wstring();
        if (isSkippedName(name)) continue;
        if (isHiddenOrSystem(p)) { ++ctx.skipped; continue; }

        ++ctx.analyzed;

        std::string cat = categoryOf(ctx, p);
        fs::path target = dir / toWide(cat);
        if (ctx.opt->byDate) {
            WORD y = 0, m = 0;
            if (lastWriteParts(p, y, m)) {
                wchar_t sub[16];
                swprintf(sub, 16, L"%04u", (unsigned)y);
                wchar_t sub2[8];
                swprintf(sub2, 8, L"%02u", (unsigned)m);
                target = target / sub / sub2;
            }
        }
        fs::path dest = reserveUnique(ctx, target / name);
        ctx.moves.push_back({ p, dest, cat });
    }
}

inline Plan planificar(const fs::path& dir, const Options& opt) {
    Context ctx;
    ctx.opt = &opt;
    for (const auto& d : defaultCats()) {
        ctx.catNamesLower.insert(toUtf8(lowerW(toWide(d.name))));
        for (auto e : d.exts) ctx.extToCat[std::string(".") + e] = d.name;
    }
    ctx.catNamesLower.insert(toUtf8(lowerW(toWide(kOther()))));
    for (const auto& r : opt.customRules) {
        ctx.extToCat[r.ext] = r.cat;
        ctx.catNamesLower.insert(toUtf8(lowerW(toWide(r.cat))));
    }

    collectDir(ctx, dir, 0);

    Plan plan;
    plan.moves = std::move(ctx.moves);
    plan.analyzed = ctx.analyzed;
    plan.skipped = ctx.skipped;
    for (const auto& m : plan.moves) plan.porCategoria[m.cat]++;
    return plan;
}

// ============================ aplicar =======================================

inline std::string csvField(const std::string& in) {
    bool need = in.find_first_of(",\"\r\n") != std::string::npos;
    if (!need) return in;
    std::string out = "\"";
    for (char ch : in) { if (ch == '"') out += "\"\""; else out += ch; }
    out += "\"";
    return out;
}

struct ApplyResult {
    long long moved = 0;
    long long errors = 0;
    std::vector<Move> done;
    fs::path log;
    std::string logName;
};

// cb(movimiento, ok, mensajeDeError)
inline ApplyResult aplicar(const Plan& plan, const fs::path& dir,
                           const std::function<void(const Move&, bool, const std::string&)>& cb) {
    ApplyResult r;
    for (const auto& m : plan.moves) {
        std::error_code e1;
        fs::create_directories(m.to.parent_path(), e1);
        fs::rename(m.from, m.to, e1);
        if (e1) {
            ++r.errors;
            if (cb) cb(m, false, e1.message());
        } else {
            ++r.moved;
            r.done.push_back(m);
            if (cb) cb(m, true, "");
        }
    }

    r.logName = ".organizador-log-" + nowStamp() + ".csv";
    r.log = dir / toWide(r.logName);
    std::ofstream f(r.log, std::ios::binary);
    if (f) {
        f << "accion,ruta_original,ruta_destino,fecha\r\n";
        std::string ts = nowStamp();
        for (const auto& m : r.done) {
            f << "MOVE," << csvField(toUtf8(m.from.wstring())) << ","
              << csvField(toUtf8(m.to.wstring())) << "," << csvField(ts) << "\r\n";
        }
    }
    return r;
}

// ============================ CSV / deshacer ================================

inline std::vector<std::vector<std::string>> parseCsv(const std::string& text) {
    std::vector<std::vector<std::string>> rows;
    std::vector<std::string> fields;
    std::string field;
    bool inQuotes = false;
    for (size_t i = 0; i < text.size(); ++i) {
        char ch = text[i];
        if (inQuotes) {
            if (ch == '"') {
                if (i + 1 < text.size() && text[i + 1] == '"') { field += '"'; ++i; }
                else inQuotes = false;
            } else field += ch;
        } else {
            if (ch == '"') inQuotes = true;
            else if (ch == ',') { fields.push_back(field); field.clear(); }
            else if (ch == '\n') { fields.push_back(field); field.clear(); rows.push_back(fields); fields.clear(); }
            else if (ch == '\r') { }
            else field += ch;
        }
    }
    if (!field.empty() || !fields.empty()) { fields.push_back(field); rows.push_back(fields); }
    return rows;
}

struct UndoResult { long long restored = 0, skipped = 0, errors = 0; };

// cb(rutaOrigen, ok)
inline UndoResult deshacer(const fs::path& logFile, const std::function<void(const fs::path&, bool)>& cb) {
    UndoResult r;
    std::ifstream f(logFile, std::ios::binary);
    if (!f) return r;
    std::ostringstream ss;
    ss << f.rdbuf();
    auto rows = parseCsv(ss.str());

    bool first = true;
    for (const auto& row : rows) {
        if (row.size() < 3) continue;
        if (first) { first = false; continue; }
        if (row[0] != "MOVE") continue;
        fs::path from = toWide(row[1]);
        fs::path to   = toWide(row[2]);

        std::error_code e;
        if (!fs::exists(to, e)) { ++r.skipped; continue; }
        if (fs::exists(from, e)) { ++r.skipped; continue; }
        fs::create_directories(from.parent_path(), e);
        fs::rename(to, from, e);
        if (e) { ++r.errors; if (cb) cb(from, false); }
        else   { ++r.restored; if (cb) cb(from, true); }
    }
    return r;
}

// ============================ utilidades varias =============================

// Mapa extension -> categoria (incluye reglas personalizadas)
inline std::map<std::string, std::string> mapaExtensiones(const Options& opt) {
    std::map<std::string, std::string> m;
    for (const auto& d : defaultCats())
        for (auto e : d.exts) m[std::string(".") + e] = d.name;
    for (const auto& r : opt.customRules) m[r.ext] = r.cat;
    return m;
}

// Categoria de un nombre de archivo segun su extension
inline std::string categoriaDeNombre(const std::map<std::string, std::string>& m, const std::string& nombre) {
    size_t p = nombre.find_last_of('.');
    if (p == std::string::npos || p + 1 >= nombre.size()) return kOther();
    std::string ext = toUtf8(lowerW(toWide(nombre.substr(p))));
    auto it = m.find(ext);
    return (it == m.end()) ? std::string(kOther()) : it->second;
}

struct FileInfo { std::uintmax_t size = 0; bool ok = false; };

inline FileInfo infoDe(const fs::path& p) {
    FileInfo fi;
    WIN32_FILE_ATTRIBUTE_DATA fad{};
    if (GetFileAttributesExW(p.c_str(), GetFileExInfoStandard, &fad)) {
        fi.size = ((std::uintmax_t)fad.nFileSizeHigh << 32) | fad.nFileSizeLow;
        fi.ok = true;
    }
    return fi;
}

inline std::wstring humanSize(std::uintmax_t b) {
    const wchar_t* u[] = { L"B", L"KB", L"MB", L"GB", L"TB" };
    double v = (double)b;
    int i = 0;
    while (v >= 1024.0 && i < 4) { v /= 1024.0; ++i; }
    wchar_t buf[48];
    if (i == 0) swprintf(buf, 48, L"%llu B", (unsigned long long)b);
    else        swprintf(buf, 48, L"%.1f %s", v, u[i]);
    return buf;
}

} // namespace org
