#include <stdlib.h>
#include <math.h>
#include <stdio.h>
int test (int a, int b, int c, int d);
int main() {
int a ;
printf(" ***** mreg0101 ***** \n") ;
a = test(1,2,3,4) ;
if( a == 7056 )  
  printf(" ** OK ** \n") ;
else
  printf(" ** NG ** a = %d \n",a) ;
exit (0);
}
int test (int a, int b, int c, int d)
{
  if( a == b ) {
     c = a + b ;
     d = c * a ;
  } else {
     a = c + b*c ;
     c = a - c ;
  }
     a = c * c + d ;
  if( a != b ) {
     c = a + b ;
     a = c * a ;
  } else {
     a = c + b*c ;
     c = a - c ;
  }
     a = c * c * d ;
  return(a) ;
}