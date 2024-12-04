#include <stdlib.h>
#include <math.h>
#include <stdio.h>

int main()
{
	float a[20][20],b[20][20],c[20][20];

	long int nn=20;
	long int i,j;
	for(j=0;j<20;j++){
		for(i=0;i<20;i++){
			a[j][i]=1.0;
			b[j][i]=2.0;
		}
	}
	for(j=0;j<20;j++){
		for(i=0;i<20;i++){
			c[j][i]=3.0;
		}
	}
	for(i=nn-1;i>=10;i-=1)
	{
		for(j=0;j<nn;j++)
		{
			a[j][i]=b[j][i-10]*c[j][i];
		}
		for(j=0;j<nn;j++)
		{
			b[j][i]=a[j][i-10]+c[j][i];
		}
	}
	printf("  ***  *** \n");
	for(j=0;j<10;j++){
		for(i=0;i<10;i++){
			printf("a[%ld][%ld]=%g , b[%ld][%ld]=%g\n",j,i,a[j][i],
			    j,i,b[j][i]);
		}
	}
	exit (0);
}