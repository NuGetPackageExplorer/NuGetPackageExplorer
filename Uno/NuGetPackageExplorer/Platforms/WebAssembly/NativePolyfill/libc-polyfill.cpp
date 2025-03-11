#include <stdlib.h>

#define WASM_EXPORT __attribute__((visibility("default")))

extern "C" {
    WASM_EXPORT int gnu_get_libc_version();
}

WASM_EXPORT int gnu_get_libc_version() {
    return 0;
}