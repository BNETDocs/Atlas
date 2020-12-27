#!/usr/bin/env python3
import socket
from struct import *

PKT_SERVERPING = 0x05
PKT_CONNTEST2 = 0x09

addr = '10.0.1.11'
port = 6112
buf  = pack('iii', PKT_CONNTEST2, 0, 0)

endp = (addr, port)
sock = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)

# sending twice mimics official client behavior, but
# it's also a good thing to do for udp datagrams.

sock.sendto(buf, endp)
sock.sendto(buf, endp)

