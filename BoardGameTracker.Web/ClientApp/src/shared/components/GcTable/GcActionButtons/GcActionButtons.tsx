import {Popconfirm, Space, Tooltip} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {DeleteOutlined, EditOutlined} from '@ant-design/icons';

import {Play} from '../../../models';

type Props = {
  id: number;
  title: string;
  description: string;
  edit: (id: number) => void
  remove: (id: number) => void
}

export const GcActionButtons = (props: Props) => {
  const { id, title, description, edit, remove } = props;
  const { t } = useTranslation();

  return (
    <Space>
      <Tooltip title="Edit" placement="topRight">
        <EditOutlined onClick={() => edit(id)} />
      </Tooltip>
      <Popconfirm
        placement="left"
        title={title}
        description={description}
        onConfirm={() => remove(id)}
        okText={t('common.yes')}
        cancelText={t('common.no')}
      >
        <DeleteOutlined style={{ color: '#a8071a' }} />
      </Popconfirm>
    </Space >
  )
}
