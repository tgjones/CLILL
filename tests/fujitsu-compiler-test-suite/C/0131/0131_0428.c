#include <stdlib.h>
#include <stdio.h>
#include <math.h>

int main()
{
	int i,n=4,a[10];
	for(i=8;i>=n--;i-=2)
	{
		a[i] = i ;
	}
	for(i=8;i>=0;i-=2){
		printf(" %d \n",a[i]);
	}
	exit (0);
}