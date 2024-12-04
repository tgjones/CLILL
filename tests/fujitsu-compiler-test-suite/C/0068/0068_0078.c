#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#ifdef _OPENMP
#include <omp.h>
#endif

int main() { 
      int ia, ib, ic, id, ie[50];
      int i, loop=50;
      ia = 0;
      ib = 0;
      for (i=0; i<loop; i+=1){ 
         ie[i] = 13*i/3*pow(-1,i);
      }
#pragma omp parallel
{
#pragma omp for reduction(+:ia,ib)
      for (i=0; i<loop; i+=1){ 
         ia = ia-i*2;
         ib = ib+i*3+ie[i];
      }
}
      ic = 0;
      id = 0;
      for (i=0; i<loop; i+=1){ 
         ic = ic-i*2;
         id = id+i*3+ie[i];
      }
      printf("-----  --");
      printf(" parallel");
      printf(" for reduction(+:ia,ib) -----\n");
      if(ia==ic && ib==id) {
         printf( "OK\n");
      }else{
         printf( "NG!  REDUCTION(+) clause is incorrect\n");
         printf( "     ia=%d,  It must be %d\n",ia,ic);
         printf( "     ib=%d,  It must be %d\n",ib,id);
      }
      exit (0) ;
      }