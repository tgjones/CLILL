#include <stdlib.h>
#include <stdio.h>
#include <math.h>
void fatal_error (void);
int main()
{
  int i;
  int m2=-2, m1=-1, ze=0, one=1, two=2, five=5;
  int eig=8, nine=9, ten=10, ele=11;
  for (i = 9; i >= 0; i--)
    {
      if (i < -1) fatal_error ();
      if (i <= -1) fatal_error ();
      if (i <   0) fatal_error ();
      if (i <=  0) printf("OK\n");
      if (i <   1) printf("OK\n");
      if (i <=  1) printf("OK\n");
      if (i <=  5) printf("OK\n");
      if (i <=  8) printf("OK\n");
      if (i <   9) printf("OK\n");
      if (i <=  9) printf("OK\n");
      if (i <  10) printf("OK\n");
      if (i <= 10) printf("OK\n");
      if (m1 > i) fatal_error ();
      if (m1 >= i) fatal_error ();
      if (ze >  i) fatal_error ();
      if (ze >= i) printf("OK\n");
      if (one > i) printf("OK\n");
      if (one >= i) printf("OK\n");
      if (five >= i) printf("OK\n");
      if (eig >= i) printf("OK\n");
      if (nine > i) printf("OK\n");
      if (nine >= i) printf("OK\n");
      if (ten >  i) printf("OK\n");
      if (ten >= i) printf("OK\n");
      if (i > -1) fatal_error ();
      if (i >= -1) fatal_error ();
      if (i >   0) fatal_error ();
      if (i >=  0) printf("OK\n");
      if (i >   1) printf("OK\n");
      if (i >=  1) printf("OK\n");
      if (i >=  5) printf("OK\n");
      if (i >=  8) printf("OK\n");
      if (i >   9) printf("OK\n");
      if (i >=  9) printf("OK\n");
      if (i >  10) printf("OK\n");
      if (i >= 10) printf("OK\n");
      if (m1 < i) fatal_error ();
      if (m1 <= i) fatal_error ();
      if (ze <  i) fatal_error ();
      if (ze <= i) printf("OK\n");
      if (one < i) printf("OK\n");
      if (one <= i) printf("OK\n");
      if (five <= i) printf("OK\n");
      if (eig <= i) printf("OK\n");
      if (nine < i) printf("OK\n");
      if (nine <= i) printf("OK\n");
      if (ten <  i) printf("OK\n");
      if (ten <= i) printf("OK\n");
    }
  if (i != -1)
    fatal_error ();
exit (0);
}
void fatal_error ()
{
  printf("NG\n");
}