#include <stdio.h>

int main(void)
{
  int i;
  const short init = 10000;
  short sum1 = init, sum2 = init;
  for (i=1; i<=100; i++) {
    sum1 = sum1 - (short)i;
  }
#pragma omp simd reduction(-:sum2)
  for (i=1; i<=100; i++) {
    sum2 = sum2 - (short)i;
  }
  if (sum1 != sum2) {
    printf("NG %d %d\n", sum1, sum2);
    return 1;
  }
  printf("OK\n");
  return 0;
}