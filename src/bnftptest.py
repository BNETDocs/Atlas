#!/usr/bin/env python3
"""Fetches one file from a running Atlas over BNFTP version 1 and version 2 and checks that both succeed with the same content.

usage: bnftptest.py [host] [port] [filename]
"""
import socket
import sys
from struct import pack, unpack_from

PLATFORM_IX86 = 0x49583836
PRODUCT_W3DM = 0x5733444d

host = sys.argv[1] if len(sys.argv) > 1 else '127.0.0.1'
port = int(sys.argv[2]) if len(sys.argv) > 2 else 6112
name = (sys.argv[3] if len(sys.argv) > 3 else 'ver-IX86-1.mpq').encode() + b'\0'


def read_all(sock):
    data = b''
    while True:
        chunk = sock.recv(65536)
        if not chunk:
            return data
        data += chunk


def parse_reply(data):
    header_length, type_, size = unpack_from('<HHI', data, 0)
    body = data[header_length:]
    assert type_ == 0, f'type {type_}'
    assert len(body) == size, f'header says {size} bytes, got {len(body)}'
    return body


def fetch_v1():
    request = pack('<HIIIIIQ', 0x100, PLATFORM_IX86, PRODUCT_W3DM, 0, 0, 0, 0) + name
    request = pack('<H', len(request) + 2) + request
    sock = socket.create_connection((host, port), timeout=5)
    sock.sendall(b'\x02' + request)
    return parse_reply(read_all(sock))


def fetch_v2():
    header = pack('<HHIIII', 20, 0x200, PLATFORM_IX86, PRODUCT_W3DM, 0, 0)
    sock = socket.create_connection((host, port), timeout=5)
    sock.sendall(b'\x02' + header)

    token = sock.recv(4)
    assert len(token) == 4, 'no server token'

    request = pack('<IQIIIII', 0, 0, 1, 0, 0, 0, 0) + bytes(20) + name
    sock.sendall(request)
    return parse_reply(read_all(sock))


v1 = fetch_v1()
v2 = fetch_v2()
assert v1 == v2, 'version 1 and version 2 returned different content'
print(f'ok: {len(v1)} bytes over both versions')
