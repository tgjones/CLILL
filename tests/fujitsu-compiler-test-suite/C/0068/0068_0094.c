#include <stdio.h>
#include <stdlib.h>
#include <math.h>
#ifdef _OPENMP
#include <omp.h>
#endif

      int sub(int ia,int ie[50],int i)
      {
      ia = ia * (fmod(i,5)+1);
      return ia;
      }
      int sub2(int ib,int ie[50],int i)
      {
      ib = ib * ie[i];
      return ib;
      }

      int ia, ib, ic, id, ie[50];
int main() { 
      int i,loop=50;
      ia = -1;
      ib = 3;
      for (i=0; i<loop; i+=1){ 
         ie[i] = (fmod(13*i,5)+1)*pow(-1,fmod(i,2));
      }
#pragma omp parallel
{
#pragma omp for reduction(*:ia,ib)
      for (i=1; i<=loop; i+=3){ 
          ia=sub (ia, ie, i);
          ib=sub2 (ib, ie, i);
      }
}
      ic = -1;
      id = 3;
      for (i=1; i<=loop; i+=3){ 
         ic = ic * (fmod(i,5)+1);
         id = id * ie[i];
      }
      printf( " ic=%d,  id= %d\n",ic,id);
      printf( "-----  --");
      printf( " parallel");
      printf( " for reduction(*:ia,ib) -----\n");
      if(ia==ic && ib==id) {
         printf( "OK\n");
      }else{
         printf( "NG! REDUCTION(*) clause is incorrect\n");
         printf( "     ia=%d,  It must be %d\n",ia,ic);
         printf( "     ib=%d,  It must be %d\n",ib,id);
      }
      exit (0) ;
      }