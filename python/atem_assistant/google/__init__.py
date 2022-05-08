#!/usr/bin/env python
# coding=utf-8

from __future__ import division, absolute_import, print_function

import logging
import os.path
import pickle

from google.auth.transport.requests import Request
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build

API_SERVICE_NAME = "youtube"
API_VERSION = "v3"
SCOPES = ["https://www.googleapis.com/auth/youtube"]

logger = logging.getLogger("atem_assistant.google")


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
