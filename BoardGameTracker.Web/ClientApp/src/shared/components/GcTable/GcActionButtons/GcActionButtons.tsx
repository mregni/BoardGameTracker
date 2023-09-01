import {Popconfirm, Space, Tooltip} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {DeleteOutlined, EditOutlined} from '@ant-design/icons';

import {Play} from '../../../models';

type Props = {
  play: Play
  editPlay: (id: number) => void
  deletePlay: (id: number) => void
}

export const GcActionButtons = (props: Props) => {
  const { play, editPlay, deletePlay } = props;
  const {t} = useTranslation();
   
  return (
    <Space>
      <Tooltip title="Edit" placement="topRight">
        <EditOutlined onClick={() => editPlay(play.id)} />
      </Tooltip>
      <Popconfirm
        placement="left"
        title={t('play.delete.title')}
        description={t('play.delete.description')}
        onConfirm={() => deletePlay(play.id)}
        okText={t('common.yes')}
        cancelText={t('common.no')}
      >
        <DeleteOutlined style={{ color: '#a8071a' }} />
      </Popconfirm>
    </Space >
  )
}
