#include <stdio.h>
struct t { int c; int a[2]; int d; } ;
struct s { struct t x; } ;
  int a = 10;
int main()
{
  struct s x = { {.a[1]=1,7} };
  printf("%d\n", x.x.a[0]);
  printf("%d\n", x.x.a[1]);
  printf("%d\n", x.x.a[2]);
  int a = 10;
  x = (struct s){ {.a[1]=2,a} };
  printf("%d\n", x.x.a[0]);
  printf("%d\n", x.x.a[1]);
  printf("%d\n", x.x.a[2]);
}