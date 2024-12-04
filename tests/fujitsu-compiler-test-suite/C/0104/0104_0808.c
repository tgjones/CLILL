#include <stdio.h>
#include <stdlib.h>

#define PRINT_NG puts("NG")
#define PRINT_OK puts("OK")
#define MAINF main

#if defined(ROLL_TIMES)
#define N ROLL_TIMES
#elif defined(MOD)
#define N 65
#else
#define N 64
#endif

void init(long long * dest) {
  int i;
#pragma clang loop vectorize(disable)
  for (i = 0; i < N; i++) {
    dest[i] = 0;
  }
}

void test1(long long * restrict dest) {
  int i;
  for(i = 0;i < N;i++ ) {
    dest[i] = 1;
  }
}

int MAINF() {
  long long dest[N];
  int i;
  init(dest);

  for (i = 0;i < 2;i++ ) {
    test1(dest);
  }
  for (i = 0;i < N;i++) {
    if (dest[i] != 1) {
      PRINT_NG;
    }
  }

  PRINT_OK;
  return 0;
}