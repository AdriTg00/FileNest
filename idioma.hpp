#pragma once

// ---------------------------------------------------------------------------
//  Idioma de la interfaz
//
//  - Por defecto: espanol si Windows esta en espanol, ingles en cualquier otro
//    caso (se mira el idioma de la interfaz del usuario, no el del sistema).
//  - Se puede forzar con la variable de entorno:  FILENEST_LANG=es  |  en
// ---------------------------------------------------------------------------

#include <windows.h>

inline bool filenestEspanol() {
    static const bool espanol = [] {
        wchar_t v[8] = {};
        DWORD n = GetEnvironmentVariableW(L"FILENEST_LANG", v, 8);
        if (n > 0 && n < 8) {
            if ((v[0] == L'e' || v[0] == L'E') && (v[1] == L's' || v[1] == L'S')) return true;
            if ((v[0] == L'e' || v[0] == L'E') && (v[1] == L'n' || v[1] == L'N')) return false;
        }
        return PRIMARYLANGID(GetUserDefaultUILanguage()) == LANG_SPANISH;
    }();
    return espanol;
}

// Texto de la interfaz:  T(L"espanol", L"english")
inline const wchar_t* T(const wchar_t* es, const wchar_t* en) {
    return filenestEspanol() ? es : en;
}

// Igual, pero en UTF-8 (lo usa el motor compartido)
inline const char* T8(const char* es, const char* en) {
    return filenestEspanol() ? es : en;
}

// Hace que Windows cargue tambien los RECURSOS en el idioma elegido: asi los
// dialogos de `recursos/dialogos.rc` (que estan definidos en los dos idiomas)
// salen en el correcto. Se llama una vez al arrancar, en el hilo de la ventana.
inline void filenestAplicarIdiomaAlHilo() {
    const bool es = filenestEspanol();
    LANGID id = es ? MAKELANGID(LANG_SPANISH, SUBLANG_SPANISH_MODERN)
                   : MAKELANGID(LANG_ENGLISH, SUBLANG_ENGLISH_US);
    SetThreadUILanguage(id);
}
