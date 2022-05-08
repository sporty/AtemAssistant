#!/usr/bin/env python
# coding=utf-8

from __future__ import absolute_import, division, print_function

import logging
import os.path
import sys

from atem_assistant.google import (
    auth,
    upload_thumbnail,
    print_categories,
    print_playlists,
    insert_broadcast,
    insert_stream
)

logger = logging.getLogger("atem_assistant")


def this_dir(filename=""):
    path = os.path.dirname(__file__)

    return os.path.normpath(os.path.abspath(os.path.join(path, filename)))


def create_youtube_live(
        client_secret,
        broadcast_title, description, start_time, privacy_status,
        stream_title,
        thumbnail,
        playlist_id, category,
):
    youtube = auth(client_secret)

    print_categories(youtube)
    print_playlists(youtube)

    broadcast_id = insert_broadcast(youtube, broadcast_title, description, start_time,
                                    privacy_status)
    stream_name = insert_stream(youtube, broadcast_id, stream_title)

    upload_thumbnail(youtube, broadcast_id, thumbnail)

    return stream_name


def main():
    import argparse
    parser = argparse.ArgumentParser(description=u"ATEM assistant")
    parser.add_argument(
        u"--google",
        dest=u"client_secret",
        type=str, default=this_dir("../../google.json"),
        metavar=u"VALUE",
        help=u"Google client secret json file.")

    parser.add_argument(
        "--broadcast-title",
        dest="broadcast_title",
        help="Broadcast title",
        default="New Broadcast")
    parser.add_argument(
        "--broadcast-description",
        dest="broadcast_description",
        help="Broadcast description",
        default="This is New Broadcast")
    parser.add_argument(
        "--privacy-status",
        dest="privacy_status",
        help="Broadcast privacy status (private|public)",
        default="private")
    parser.add_argument(
        "--start-time",
        dest="start_time",
        help="Scheduled start time",
        default='2022-05-30T00:00:00.000Z')
    parser.add_argument(
        "--stream-title",
        dest="stream_title",
        help="Stream title",
        default="New Stream")

    parser.add_argument(
        "--playlist-id",
        dest="playlist_id",
        help="Play list id",
        default="PLyhlpUpA8tqyvSstYYdNQImXrVC98YnCH")
    parser.add_argument(
        "--category",
        dest="category",
        help="Category id",
        default="22")
    parser.add_argument(
        "--thumbnail",
        dest="thumbnail",
        help="Stream title",
        default=this_dir("../../images/thumbnail.png"))

    parser.add_argument(
        u"-D", u"--debug",
        dest=u"debug",
        action=u"store_true", default=False,
        help=u"Debug mode.")
    args = parser.parse_args()

    # Init logger
    if args.debug:
        _log_format = u"%(asctime)s %(name)s [%(levelname)s] %(filename)s:%(lineno)d %(message)s"
        _log_level = logging.DEBUG
    else:
        _log_format = u"[%(levelname)s] %(message)s"
        _log_level = logging.INFO
    _formatter = logging.Formatter(_log_format)
    _stream_handler = logging.StreamHandler()
    _stream_handler.setFormatter(_formatter)
    _logger = logging.getLogger(u"atem_assistant")
    _logger.handlers = []
    _logger.addHandler(_stream_handler)
    _logger.setLevel(_log_level)

    # Create YouTube live stream
    stream_name = create_youtube_live(
        args.client_secret,
        args.broadcast_title, args.broadcast_description,
        args.start_time, args.privacy_status,
        args.stream_title,
        args.thumbnail,
        args.playlist_id, args.category,
    )
    logger.info("stream: {0}".format(stream_name))

    # Upload ATEM setting
    pass

    return 0


if __name__ == u"__main__":
    sys.exit(main())
