#include <stdlib.h>

#include <stdio.h>
#include <math.h>
int sub1(long int b, long int *l);
int main (void)
{
  long int a,c=100,l=3,i;
  long int b[100]={5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
                   5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
                   5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
                   5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
	           5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5};
  long int n[100]={3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                   3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                   3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                   3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
                   3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3};

  a=c/3;
  for ( i=99; i>=0; i-- ){
    l=i;
    if (a<50) sub1(n[i],&l);
    b[i]=b[i]+l*i;    
  }
  for ( i=0; i<100; i++ ) printf("b[%ld] = %ld\n",i,b[i]);
  exit(0);
}

int sub1(long int b, long int *l)
{
  long int i;
  for ( i=1; i<100; i++ ) *l=*l+b/i;
}