import {Button} from 'antd';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {ArrowLeftOutlined} from '@ant-design/icons';

type Props = {
  onClick: () => void;
  disabled?: boolean;
}

const GcBackButton = (props: Props) => {
  const {onClick, disabled = false} = props;
  const { t } = useTranslation();
  return (
    <Button
      style={{ position: 'absolute', top: 0, right: 0 }}
      type="dashed"
      icon={<ArrowLeftOutlined />}
      size="small"
      onClick={onClick}
      disabled={disabled}
    >
      {t('common.back')}
    </Button>
  )
}

export default GcBackButton