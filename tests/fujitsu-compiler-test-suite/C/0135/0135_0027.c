#include <stdlib.h>


#include <stdio.h>
#include <math.h>

int main()
{
  int        i,j,n=100;
  float      a[100],b[100];
  float      y=0.0;

  for(i=0;i<100;i++) a[i]=b[i]=.0;
  a[0]=b[0]=1.0;
  a[1]=b[1]=2.0;
  for(i=2;i<n;i+=2) {    
    a[i]=i;
    b[i]=a[i];
    a[i]=a[i-2]+y;
    y+=n;
    b[i]+=i;
  }
  printf("##  ##\n");
  for(i=0;i<100;i++) {
    printf("%f ",a[i]);
    j=i%9;
    if(j==0 && i!=0) printf("\n");
  }
  for(i=0;i<100;i++) {
    printf("%f ",b[i]);
    j=i%9;
    if(j==0 && i!=0) printf("\n");
  }
  exit(0);
}