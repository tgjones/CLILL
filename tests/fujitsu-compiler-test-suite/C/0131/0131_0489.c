#include <stdlib.h>
#include <stdio.h>
#include <math.h>

int main(){
	long int    a[10] = {
		0,0,0,0,0,0,0,0,0,0	};
	int    i , m1 = 7 , m2 = 2 ,m3 = 3 , m4 = 1;
	for(i = 0 ; i < 10 ; i++){
		a[i +(m1 - (m2 + (m3 + (m4 * 2))))] = i ;
	}
	for(i=0 ; i < 10 ; i++)
	{
		printf(" a[%d] = %ld \n",i,a[i]);
	}
	exit (0);
}
