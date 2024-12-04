#include <stdio.h>
#include <stdlib.h>
#include <complex.h>

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


void test(float _Complex *dest, double _Complex *a, double _Complex *b) {
  int i;
  for(i = 0; i < N; i++ ) {
    dest[i] = a[i] != b[i] ? (0.0F + 0.0iF) : (1.0F + 1.0iF);
  }
}

int MAINF() {
  int i;
  float _Complex dest[N];
  double _Complex a[N], b[N];
  for (i = 0; i < N; i++) {
    a[i] = 1.0L + 1.0iL;
    b[i] = 1.0L + 1.0iL;
  }

  test(dest, a, b);
  for (i = 0; i < N; i++) {
    if (dest[i] != 1.0F + 1.0iF) {
      PRINT_NG;
    }
  }
  PRINT_OK;
  return 0;
}