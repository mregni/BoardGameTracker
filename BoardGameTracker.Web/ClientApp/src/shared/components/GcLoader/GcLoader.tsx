import {Space, Spin} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {blue} from '@ant-design/colors';

type Props = {}

export const GcLoader = (props: Props) => {
  const { t } = useTranslation();

  return (
    <Space direction='vertical' style={{display: 'flex', justifyContent: 'center', alignItems: 'center', width: '100vw', height: '100vh'}}>
      <Spin size="large" />
      <span style={{color: blue[6]}}>{t('common.loading')}</span>
    </Space>
  )
}
