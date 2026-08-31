#define _GNU_SOURCE

#include <arpa/inet.h>
#include <errno.h>
#include <netinet/in.h>
#include <stddef.h>
#include <stdlib.h>
#include <sys/socket.h>
#include <sys/syscall.h>
#include <unistd.h>

static int address_is_allowed(const struct sockaddr *address, socklen_t length)
{
    if (address == NULL || address->sa_family == AF_UNIX || address->sa_family == AF_UNSPEC) {
        return 1;
    }

    if (address->sa_family == AF_INET && length >= sizeof(struct sockaddr_in)) {
        const struct sockaddr_in *ipv4 = (const struct sockaddr_in *)address;
        const unsigned char *octets = (const unsigned char *)&ipv4->sin_addr.s_addr;
        if (octets[0] == 127) {
            return 1;
        }

        const char *allowed_value = getenv("QDB_E2E_ALLOWED_IPV4");
        struct in_addr allowed;
        return allowed_value != NULL
            && inet_pton(AF_INET, allowed_value, &allowed) == 1
            && allowed.s_addr == ipv4->sin_addr.s_addr;
    }

    if (address->sa_family == AF_INET6 && length >= sizeof(struct sockaddr_in6)) {
        const struct sockaddr_in6 *ipv6 = (const struct sockaddr_in6 *)address;
        return IN6_IS_ADDR_LOOPBACK(&ipv6->sin6_addr);
    }

    return 0;
}

static int reject_disallowed(const struct sockaddr *address, socklen_t length)
{
    if (address_is_allowed(address, length)) {
        return 0;
    }

    errno = EPERM;
    return -1;
}

int connect(int socket_descriptor, const struct sockaddr *address, socklen_t length)
{
    if (reject_disallowed(address, length) != 0) {
        return -1;
    }
    return syscall(SYS_connect, socket_descriptor, address, length);
}

ssize_t sendto(
    int socket_descriptor,
    const void *buffer,
    size_t length,
    int flags,
    const struct sockaddr *destination,
    socklen_t destination_length)
{
    if (destination != NULL && reject_disallowed(destination, destination_length) != 0) {
        return -1;
    }
    return syscall(
        SYS_sendto,
        socket_descriptor,
        buffer,
        length,
        flags,
        destination,
        destination_length);
}

ssize_t sendmsg(int socket_descriptor, const struct msghdr *message, int flags)
{
    if (
        message != NULL
        && message->msg_name != NULL
        && reject_disallowed(message->msg_name, message->msg_namelen) != 0) {
        return -1;
    }
    return syscall(SYS_sendmsg, socket_descriptor, message, flags);
}

int sendmmsg(int socket_descriptor, struct mmsghdr *messages, unsigned int count, int flags)
{
    for (unsigned int index = 0; index < count; index++) {
        const struct msghdr *message = &messages[index].msg_hdr;
        if (
            message->msg_name != NULL
            && reject_disallowed(message->msg_name, message->msg_namelen) != 0) {
            return -1;
        }
    }
    return syscall(SYS_sendmmsg, socket_descriptor, messages, count, flags);
}
