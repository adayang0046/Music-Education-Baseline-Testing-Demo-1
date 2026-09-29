// Hardware probe: prints only bytes received from an explicitly named source.
#include <stdio.h>
#include <string.h>
#include <unistd.h>
void *xm_create(void);
void xm_destroy(void *);
int xm_count(void);
unsigned int xm_source(int);
int xm_name(unsigned int, char *, int);
int xm_connect(void *, unsigned int);
int xm_poll(void *, unsigned char *, int, int *);
int main(int argc, char **argv) {
    if (argc != 2) { fprintf(stderr,"Usage: midi-listen 'source name'\n"); return 2; }
    void *input=xm_create();
    if (!input) return 3;
    unsigned int source=0;
    for (int i=0;i<xm_count();i++) {
        char name[1024]={0}; unsigned int candidate=xm_source(i);
        xm_name(candidate,name,sizeof(name));
        if (strcmp(name,argv[1])==0) { source=candidate; break; }
    }
    if (!source || xm_connect(input,source)!=0) { fprintf(stderr,"Source unavailable\n"); xm_destroy(input); return 4; }
    printf("Listening to %s for 30 seconds. Press and release keys.\n",argv[1]); fflush(stdout);
    int total=0;
    for (int i=0;i<3000;i++) {
        unsigned char bytes[65536]; int overflow=0;
        int count=xm_poll(input,bytes,sizeof(bytes),&overflow);
        if (count) {
            printf("Received:"); for (int j=0;j<count;j++) printf(" %02X",bytes[j]);
            printf(" (overflow %d)\n",overflow); fflush(stdout); total+=count;
        }
        usleep(10000);
    }
    printf("Total received bytes: %d\n",total); xm_destroy(input);
    return total ? 0 : 5;
}
