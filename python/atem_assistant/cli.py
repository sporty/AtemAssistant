#!/usr/bin/env python
# coding=utf-8

from __future__ import absolute_import, division, print_function

import os.path
import logging

import httplib2
import os
import sys

import pickle
import os.path
from googleapiclient.discovery import build
from google_auth_oauthlib.flow import InstalledAppFlow
from google.auth.transport.requests import Request

'''
from apiclient.discovery import build
from apiclient.errors import HttpError
from oauth2client.client import flow_from_clientsecrets
from oauth2client.file import Storage
from oauth2client.tools import argparser, run_flow
'''

API_SERVICE_NAME = "youtube"
API_VERSION = "v3"
SCOPES = ["https://www.googleapis.com/auth/youtube"]

logger = logging.getLogger("atem_assistant")


def this_dir(filename=""):
    path = os.path.dirname(__file__)

    return os.path.normpath(os.path.abspath(os.path.join(path, filename)))


def auth(client_secret, token_file="token.pickle"):
    creds = None

    if os.path.exists(token_file):
        with open(token_file, 'rb') as token:
            creds = pickle.load(token)

    # If there are no (valid) credentials available, let the user log in.
    if not creds or not creds.valid:
        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())
        else:
            flow = InstalledAppFlow.from_client_secrets_file(
                client_secret, SCOPES)
            creds = flow.run_local_server(port=0)

        # Save the credentials for the next run
        with open(token_file, 'wb') as token:
            pickle.dump(creds, token)

    return build(API_SERVICE_NAME, API_VERSION, credentials=creds)


def upload_thumbnail(youtube, video_id, file):
    response = youtube.thumbnails().set(
        videoId=video_id,
        media_body=file
    ).execute()

    return response


def print_categories(youtube):
    response = youtube.videoCategories().list(
        part="id,snippet",
        regionCode="JP"
    ).execute()

    logger.info("Categories:")
    for item in response["items"]:
        logger.info("{0} : {1}".format(item["id"], item["snippet"]["title"]))

    return response


def print_playlists(youtube):
    response = youtube.playlists().list(
        part="id,snippet,status",
        mine=True
    ).execute()

    logger.info("My Playlists:")
    for item in response["items"]:
        logger.info("{0} : {1}".format(item["id"], item["snippet"]["title"]))

    return response


def add_to_playlist(youtube, playlist_id, video):
    logger.debug(video)

    response = youtube.playlistItems().insert(
        part="snippet",
        body=dict(
            snippet=dict(
                playlistId=playlist_id,
                resourceId=video,
            ),
        )
    ).execute()

    return response


# Create a liveBroadcast resource and set its title, scheduled start time,
# scheduled end time, and privacy status.
def insert_broadcast(
        youtube, broadcast_title, description, start_time, privacy_status
):
    insert_broadcast_response = youtube.liveBroadcasts().insert(
        part="snippet,status,contentDetails",
        body=dict(
            snippet=dict(
                title=broadcast_title,
                description=description,
                scheduledStartTime=start_time,
            ),
            status=dict(
                privacyStatus=privacy_status,
                selfDeclaredMadeForKids=False
            ),
            contentDetails=dict(
                enableAutoStart=True,
                enableAutoStop=True,
            )
        )
    ).execute()

    snippet = insert_broadcast_response["snippet"]

    logger.info("Broadcast '{0}' with title '{1}' was published at '{2}'.".format(
        insert_broadcast_response["id"], snippet["title"], snippet["publishedAt"]))

    return insert_broadcast_response["id"]


# Create a liveStream resource and set its title, format, and ingestion type.
# This resource describes the content that you are transmitting to YouTube.
def insert_stream(youtube, broadcast_id, stream_title):
    response = youtube.liveStreams().insert(
        part="snippet,cdn",
        body=dict(
            snippet=dict(
                title=stream_title
            ),
            cdn=dict(
                ingestionType="rtmp",
                resolution="1080p",
                frameRate="60fps",
            )
        )
    ).execute()

    snippet = response["snippet"]

    logger.info("Stream '{0}' with title '{1}' was inserted.".format(
        response["id"], snippet["title"]))

    bind_broadcast(youtube, broadcast_id, response["id"])

    return response["cdn"]["ingestionInfo"]["streamName"]


# Bind the broadcast to the video stream. By doing so, you link the video that
# you will transmit to YouTube to the broadcast that the video is for.
def bind_broadcast(youtube, broadcast_id, stream_id):
    bind_broadcast_response = youtube.liveBroadcasts().bind(
        part="id,contentDetails",
        id=broadcast_id,
        streamId=stream_id
    ).execute()

    logger.info("Broadcast '{0}' was bound to stream '{1}'.".format(
        bind_broadcast_response["id"],
        bind_broadcast_response["contentDetails"]["boundStreamId"])
    )


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
    logger.info("stream: {0}".format(stream_name))

    upload_thumbnail(youtube, broadcast_id, thumbnail)

    # add_to_playlist(youtube, playlist_id, broadcast_id)


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
    create_youtube_live(
        args.client_secret,
        args.broadcast_title, args.broadcast_description,
        args.start_time, args.privacy_status,
        args.stream_title,
        args.thumbnail,
        args.playlist_id, args.category,
    )

    # Upload ATEM setting
    pass

    return 0


if __name__ == u"__main__":
    sys.exit(main())
