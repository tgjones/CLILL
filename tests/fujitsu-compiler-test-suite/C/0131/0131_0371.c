#include <stdlib.h>
#include <stdio.h>
#include <math.h>

int main()
{
	int i,n=2,a[10];
	for(i=n+1;i<10;i++)
	{
		a[i] = i ;
	}
	for(i=n+1;i<10;i++){
		printf(" a[%d] => %d \n",i,a[i]);
	}
	exit (0);
}