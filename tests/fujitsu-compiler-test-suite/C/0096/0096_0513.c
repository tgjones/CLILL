#include <stdio.h>
struct str {
  union utest{
   int    member1;
   double member2;
  }un;
  int member3;
};

struct str str1;
int main() {
  struct str str2;

  str1.un.member1 = 1;
  str1.member3 = 2;
  str2 = str1;

  if(str2.un.member1 == 1){
    printf("OK\n");
  }else{
    printf("NG\n");
  }
}