#include <stdio.h>

unsigned            char   uc1,uc2,uc3,uc4;
unsigned      short int    us1,us2;
unsigned            int    ui1,ui2,ui3;
unsigned long long  int    ul1,ul2,ul3,ul4,ul5;
                    float  fl1,fl2[2];
                    double db1,db2,db3;
              long  double ld1,ld2,ld3,ld4,ld5,ld6,ld7;
int main()
{
 static signed char            sc1,sc2,sc3[3],sc4,sc5[4],sc6,sc7;
 static signed short int       ss1[4],ss2[2];
 static signed int             si1,si2[2];
 static signed long long int   sl1,sl2[3],sl3;
 static unsigned char          uchr1[10],uchr2;
 static unsigned short int     usin1,usin2[2];
 static unsigned int           uint1,uint2,uint3;
 static unsigned long long int ulin1,ulin2;
 static signed char           *p1=sc3;
 static unsigned int          *p2=&uint2;

 printf("* ENTERED *\n");
 if(uc1==0)                         printf(" ** 01 OK **\n");
                               else printf(" ** 01 NG **\n");
 if(uc2+1==1)                       printf(" ** 02 OK **\n");
                               else printf(" ** 02 NG **\n");
 if(uc3*100==0)                     printf(" ** 03 OK **\n");
                               else printf(" ** 03 NG **\n");
 if(uc4/10==0)                      printf(" ** 04 OK **\n");
                               else printf(" ** 04 NG **\n");
 if(us1-10==-10)                    printf(" ** 05 OK **\n");
                               else printf(" ** 05 NG **\n");
 if(us2 | (123==123))                 printf(" ** 06 OK **\n");
                               else printf(" ** 06 NG **\n");
 if(ui1*30==ui2*40)                 printf(" ** 07 OK **\n");
                               else printf(" ** 07 NG **\n");
 if(++ui3==1)                       printf(" ** 08 OK **\n");
                               else printf(" ** 08 NG **\n");
 if(ul1==0)                         printf(" ** 09 OK **\n");
                               else printf(" ** 09 NG **\n");
 if(++ul2==1)                       printf(" ** 10 OK **\n");
                               else printf(" ** 10 NG **\n");
 if(ul3+ul4==ul5)                   printf(" ** 11 OK **\n");
                               else printf(" ** 11 NG **\n");
 if(fl1==0)                         printf(" ** 12 OK **\n");
                               else printf(" ** 12 NG **\n");
 if(fl2[1]-fl2[0]==0)           printf(" ** 13 OK **\n");
                               else printf(" ** 13 NG **\n");
 if(db1*db2==db3)                   printf(" ** 14 OK **\n");
                               else printf(" ** 14 NG **\n");
 if(ld1==0)                         printf(" ** 15 OK **\n");
                               else printf(" ** 15 NG **\n");
 if(ld2+ld3==ld4-ld5*2)             printf(" ** 16 OK **\n");
                               else printf(" ** 16 NG **\n");
 if(ld6+123==123)                   printf(" ** 17 OK **\n");
                               else printf(" ** 17 NG **\n");
 if(ld7==0)                         printf(" ** 18 OK **\n");
                               else printf(" ** 18 NG **\n");

 if(sc1==0 && sc2==0)               printf(" ** 19 OK **\n");
                               else printf(" ** 19 NG **\n");
 if(sc3[0]+sc3[1]+sc3[2]==0)  printf(" ** 20 OK **\n");
                               else printf(" ** 20 NG **\n");
 if(123>>sc4==123 && sc4==0)        printf(" ** 21 OK **\n");
                               else printf(" ** 21 NG **\n");
 if(sc5[0]*2+2==sc5[2]+1+1)     printf(" ** 22 OK **\n");
                               else printf(" ** 22 NG **\n");
 if(sc5[1]-144==sc5[3]-200+56)  printf(" ** 23 OK **\n");
                               else printf(" ** 23 NG **\n");
 if(sc6==sc7 && sc7==0)             printf(" ** 24 OK **\n");
                               else printf(" ** 24 NG **\n");
 if(ss1[1]^(32767==ss1[0]+32767))printf(" ** 25 OK **\n");
                               else printf(" ** 25 NG **\n");
 if(ss1[2]+ss1[3]-ss2[1]==0)  printf(" ** 26 OK **\n");
                               else printf(" ** 26 NG **\n");
 if(ss2[0]^(32767-1==32766))       printf(" ** 27 OK **\n");
                               else printf(" ** 27 NG **\n");
 if(sc3[0]+sc3[1]+sc3[2]==0)  printf(" ** 28 OK **\n");
                               else printf(" ** 28 NG **\n");
 if(++si1==1 && si1-1==0)           printf(" ** 29 OK **\n");
                               else printf(" ** 29 NG **\n");
 if(si2[0]+si2[1]+1==1)         printf(" ** 30 OK **\n");
                               else printf(" ** 30 NG **\n");
 if(--sl1+(--sl3)==-2)              printf(" ** 31 OK **\n");
                               else printf(" ** 31 NG **\n");
 if(--sl2[1]+(++sl2[0])==0)     printf(" ** 32 OK **\n");
                               else printf(" ** 32 NG **\n");
 if(uchr1[7]+uchr2==0)            printf(" ** 33 OK **\n");
                               else printf(" ** 33 NG **\n");
 if(usin1==uint3 && uint1==ulin2)   printf(" ** 34 OK **\n");
                               else printf(" ** 34 NG **\n");
 if(ulin1==0)                       printf(" ** 35 OK **\n");
                               else printf(" ** 35 NG **\n");
 if(p1==sc3)                        printf(" ** 36 OK **\n");
                               else printf(" ** 36 NG **\n");
 if(p2==&uint2)                     printf(" ** 37 OK **\n");
                               else printf(" ** 37 NG **\n");
 printf("  * ENDED **\n");
}