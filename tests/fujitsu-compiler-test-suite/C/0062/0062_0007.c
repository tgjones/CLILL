#include <stdio.h>
#define N 100
#define IANS 2

void foo(double a00[N], double a01[N]) {
  int i;
  for (i=0; i<N; ++i) {
    a00[i] = a00[i]+a01[i];
  }
}

void init_a(double a00[N], double a01[N], double r) {
  int i;

  for (i=0; i<N; ++i) {
    a00[i] = r;
    a01[i] = r;
  }
}

int main(void) {
  double a00[N];
  double a01[N];

  init_a(a00,a01,1.0);
  foo(a00,a01);
  if ((int)(a00[N-1]) == IANS) {
    printf("ok\n");
  } else {
    printf("ng: %d != %d\n", (int)a00[N-1], IANS);
  }
  return 0;
}
