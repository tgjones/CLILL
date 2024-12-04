#include <stdlib.h>
#include <stdio.h>

int main()
{
  long long int  a = 2*2147483648LL;

#if defined(big_endian)
  if (   *((int *)&a)==0x1
      && *((((int *)&a))+1)==0x0 ) {
#else
  if (   *((int *)&a)==0x0
      && *((((int *)&a))+1)==0x1 ) {
#endif
    printf("OK\n");
    exit(0);
  }

  printf("NG\n");
  exit(1);
}