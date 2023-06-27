import {NotificationInstance} from 'antd/es/notification/interface';

export const createInfoNotification = (notification: NotificationInstance, message: string, description: string): void => {
  notification.info({
    message: message,
    description: description,
    placement: 'bottomRight'
  });
}

export const createSuccessNotification = (notification: NotificationInstance, message: string, description: string): void => {
  notification.success({
    message: message,
    description: description,
    placement: 'bottomRight'
  });
}

export const createWarningNotification = (notification: NotificationInstance, message: string, description: string): void => {
  notification.warning({
    message: message,
    description: description,
    placement: 'bottomRight'
  });
}

export const createErrorNotification = (notification: NotificationInstance, message: string, description: string): void => {
  notification.error({
    message: message,
    description: description,
    placement: 'bottomRight'
  });
}
