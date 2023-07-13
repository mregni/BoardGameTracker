import {Modal} from 'antd';
import {ReactNode} from 'react';

import {ExclamationCircleFilled} from '@ant-design/icons';

import i18n from '../../i18n';

const { confirm } = Modal;

export const createDeleteModal = (title: string, content: ReactNode, onDelete: () => Promise<void>): void => {
  confirm({
    title: title,
    icon: <ExclamationCircleFilled />,
    content: content,
    okText: i18n.t('common.yes'),
    okType: 'danger',
    cancelText: i18n.t('common.no'),
    cancelButtonProps: { type: 'primary'},
    onOk: onDelete,
    autoFocusButton: 'cancel',
    maskClosable: true
  });
}