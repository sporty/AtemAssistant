#!/usr/bin/env python
# coding=utf-8

from __future__ import division, absolute_import, print_function

import logging
import os.path
import subprocess
import sys

logger = logging.getLogger("atem_assistant.atem")


def resource_path(relative_path):
    u"""pyinstallerでexe化されていた場合は解凍フォルダからの相対パス"""
    if hasattr(sys, "_MEIPASS"):
        return os.path.join(getattr(sys, "_MEIPASS"), relative_path)
    return os.path.join(os.path.abspath("."), relative_path)


def set_stream_key(ip_address, new_key):
    logger.info("Set key ({1}) to ATEM ({0})".format(ip_address, new_key))

    cmd = [
        resource_path("atem_sdk\\AtemSDK\\x64\\Release\\TestAtem.exe"),
        ip_address, new_key
    ]
    logger.debug(cmd)

    result = subprocess.run(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    logger.info(result.stdout.decode(u"utf-8"))
