// Project-owned CoreMIDI bridge. Only the byte queue is touched by the MIDI thread.
#include <CoreMIDI/CoreMIDI.h>
#include <pthread.h>
#include <stdlib.h>
#include <string.h>
#define CAPACITY 65536

typedef struct {
    MIDIClientRef client;
    MIDIPortRef port;
    MIDIEndpointRef source;
    pthread_mutex_t lock;
    unsigned char bytes[CAPACITY];
    int count, overflow;
} Input;

static void receive(const MIDIPacketList *list, void *context, void *connection) {
    (void)connection;
    Input *input = context;
    pthread_mutex_lock(&input->lock);
    const MIDIPacket *packet = &list->packet[0];
    for (UInt32 i = 0; i < list->numPackets; i++) {
        if (input->count + packet->length > CAPACITY) {
            input->count = 0;
            input->overflow++;
        }
        memcpy(input->bytes + input->count, packet->data, packet->length);
        input->count += packet->length;
        packet = MIDIPacketNext(packet);
    }
    pthread_mutex_unlock(&input->lock);
}

void *xm_create(void) {
    Input *input = calloc(1, sizeof(Input));
    if (!input) return NULL;
    pthread_mutex_init(&input->lock, NULL);
    if (MIDIClientCreate(CFSTR("XR MIDI Monitor"), NULL, NULL, &input->client) != noErr ||
        MIDIInputPortCreate(input->client, CFSTR("Keyboard input"), receive, input, &input->port) != noErr) {
        if (input->client) MIDIClientDispose(input->client);
        pthread_mutex_destroy(&input->lock); free(input); return NULL;
    }
    return input;
}
int xm_count(void) { return (int)MIDIGetNumberOfSources(); }
unsigned int xm_source(int index) { return MIDIGetSource(index); }
int xm_name(unsigned int source, char *buffer, int size) {
    CFStringRef name = NULL;
    if (MIDIObjectGetStringProperty(source, kMIDIPropertyDisplayName, &name) != noErr) return 0;
    int result = CFStringGetCString(name, buffer, size, kCFStringEncodingUTF8);
    CFRelease(name); return result;
}
void xm_disconnect(void *context) {
    Input *input = context;
    if (input->source) MIDIPortDisconnectSource(input->port, input->source);
    input->source = 0;
    pthread_mutex_lock(&input->lock); input->count = 0; pthread_mutex_unlock(&input->lock);
}
int xm_connect(void *context, unsigned int source) {
    Input *input = context;
    xm_disconnect(input);
    OSStatus status = MIDIPortConnectSource(input->port, source, NULL);
    if (status == noErr) input->source = source;
    return (int)status;
}
int xm_poll(void *context, unsigned char *buffer, int size, int *overflow) {
    Input *input = context;
    pthread_mutex_lock(&input->lock);
    int count = input->count < size ? input->count : size;
    memcpy(buffer, input->bytes, count);
    memmove(input->bytes, input->bytes + count, input->count - count);
    input->count -= count; *overflow = input->overflow;
    pthread_mutex_unlock(&input->lock); return count;
}
void xm_destroy(void *context) {
    if (!context) return;
    Input *input = context;
    xm_disconnect(input);
    MIDIPortDispose(input->port);
    MIDIClientDispose(input->client);
    pthread_mutex_destroy(&input->lock); free(input);
}
