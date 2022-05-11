#!/usr/bin/env python
# coding=utf-8

from __future__ import division, absolute_import, print_function

import logging
import subprocess

logger = logging.getLogger("atem_assistant.atem")


def set_stream_key(ip_address, new_key):
    logger.info("Set key ({1}) to ATEM ({0})".format(ip_address, new_key))

    cmd = ["atem_sdk\\AtemSDK\\x64\\Release\\TestAtem.exe", ip_address, new_key]
    logger.debug(cmd)

    result = subprocess.run(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    logger.info(result.stdout.decode(u"utf-8"))
