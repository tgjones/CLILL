#include <stdlib.h>
#include <stdio.h>
#include <math.h>
int sub(long int n);

int main()
{
	sub(9);
	exit (0);
}
int sub(long int n)
{
	static int a[10],i;
	for(i=0;i<10;i++)
	{
		a[n-(-(-(-i+n)))] = i;
	}
	for(i=0;i<10;i++)
	{
		printf("a[%d] => %d \n",i,a[i]) ;
	}
}