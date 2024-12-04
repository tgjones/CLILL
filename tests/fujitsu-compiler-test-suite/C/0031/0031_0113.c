#include<stdio.h>
#include<complex.h>

#define CF _Complex float
#define CD _Complex double
#define CL _Complex long double

void dump(CF arg, int len)
{
  if(len == 8)
    printf("%f %f \n",crealf(arg),cimagf(arg));
  else if(len == 16)
    printf("%lf %lf \n",creal(arg),cimag(arg));
  else 
    printf("%Lf %Lf \n",creall(arg),cimagl(arg));
}

union A { CF a; };
static union A y;

CF foo()
{
   extern union A x; x.a = 7893457.1793 + 12345.3456i; y.a = 7893457.1793 + 12345.3456i; x.a += ( x.a == y.a )? 10.1234 : 20.1234;
   return x.a;
}

union A x;

int main()
{
   CF x = foo();
   dump(x, sizeof(x));
   return 0;
}