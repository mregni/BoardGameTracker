import {App} from 'antd';
import {ReactNode} from 'react';

import {ExclamationCircleFilled} from '@ant-design/icons';

import i18n from '../../i18n';

interface Props {
  deleteModal: (title: string, content: ReactNode, onDelete: () => Promise<void>) => void;
}

export const useModals = (): Props => {
  const { modal } = App.useApp();

  const deleteModal = (title: string, content: ReactNode, onDelete: () => Promise<void>): void => {
    modal.confirm({
      title: title,
      icon: <ExclamationCircleFilled />,
      content: content,
      okText: i18n.t('common.yes'),
      okType: 'danger',
      cancelText: i18n.t('common.no'),
      cancelButtonProps: { type: 'primary' },
      onOk: onDelete,
      autoFocusButton: 'cancel',
      maskClosable: true
    });
  }

  return {deleteModal}
}