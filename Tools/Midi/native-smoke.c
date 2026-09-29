#include <CoreMIDI/CoreMIDI.h>
#include <assert.h>
#include <stdio.h>
#include <unistd.h>
void *xm_create(void);
void xm_destroy(void *);
int xm_connect(void *, unsigned int);
int xm_poll(void *, unsigned char *, int, int *);
int xm_count(void);
unsigned int xm_source(int);
int xm_name(unsigned int, char *, int);
int main(void) {
    void *input = xm_create();
    if (!input) { fprintf(stderr, "Cannot create CoreMIDI client\n"); return 1; }
    printf("Available physical/virtual sources: %d\n", xm_count());
    for (int i=0; i<xm_count(); i++) { char name[1024]={0}; xm_name(xm_source(i), name, sizeof(name)); printf("  %s\n", name); }
    MIDIClientRef client; MIDIEndpointRef source;
    assert(MIDIClientCreate(CFSTR("XR MIDI smoke sender"), NULL, NULL, &client)==noErr);
    assert(MIDISourceCreate(client, CFSTR("XR MIDI temporary test source"), &source)==noErr);
    assert(xm_connect(input,source)==noErr);
    unsigned char storage[1024], result[1024];
    MIDIPacketList *list=(MIDIPacketList *)storage;
    MIDIPacket *packet=MIDIPacketListInit(list);
    unsigned char notes[]={0x90,60,100,0x80,60,0};
    assert(MIDIPacketListAdd(list,sizeof(storage),packet,0,sizeof(notes),notes));
    assert(MIDIReceived(source,list)==noErr);
    int count=0, overflow=0;
    for (int i=0;i<100 && count==0;i++) { usleep(10000); count=xm_poll(input,result,sizeof(result),&overflow); }
    assert(count==sizeof(notes)); assert(overflow==0);
    for (int i=0;i<count;i++) assert(result[i]==notes[i]);
    xm_destroy(input); MIDIEndpointDispose(source); MIDIClientDispose(client);
    puts("PASS: virtual CoreMIDI source -> native callback -> polled bytes; connection disposed.");
}
