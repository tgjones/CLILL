#include <stdlib.h>
#include <stdio.h>
#include <math.h>

int main()
{
	int i,n=0,a[10];
	for(i=4;i>=n;i--)
	{
		a[i] = i + 1 ;
	}
	for(i=4;i>=0;i--){
		printf(" a[%d] => %d \n",i,a[i]);
	}
	exit (0);
}