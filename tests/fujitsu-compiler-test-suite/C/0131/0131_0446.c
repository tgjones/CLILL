#include <stdlib.h>
#include <stdio.h>
#include <math.h>

int main()
{
	int i,a[11];
	for(i=0;i<=10;++i+1)
	{
		a[i] = i + 1 ;
	}
	for(i=0;i<=10;i+=2){
		printf(" a[%d] => %d \n",i,a[i]);
	}
	exit (0);
}