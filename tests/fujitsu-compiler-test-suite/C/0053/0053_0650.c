#include<stdio.h>
int f()
{
  static int a;
  a=10;
  if( a==10 )
   return 1;
  else
   return 0;
}

extern int a;
int main()
{
  if( a==5 && f() )
    printf(" TEST OK \n");
  else
    printf(" TEST NG = %d \n",a);
}
int a=5;