import {Button, Form, Input, Select} from 'antd';
import {format} from 'date-fns';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {SettingsContext} from '../../../context/settingsContext';
import {Settings} from '../../../models';

export const LocalisationForm = () => {
  const { t } = useTranslation();
  const { settings, saveSettings } = useContext(SettingsContext);
  const [form] = Form.useForm();

  const onFinish = async (values: Settings) => {
    await saveSettings({
      ...settings,
      ...values
    });
  }
  return (
    <div style={{ width: 400 }}>
      <h3>{t('settings.localisation')}</h3>
      <Form
        labelCol={{ span: 10 }}
        wrapperCol={{ span: 20 }}
        autoComplete='off'
        layout="horizontal"
        initialValues={{ ...settings }}
        onFinish={onFinish}
        form={form}
        name="localisation"
        requiredMark={false}
        style={{ width: '100%' }}
      >
        <Form.Item
          name="decimalSeparator"
          rules={[{ required: true }]}
          label={t('settings.decimal-separator')}
        >
          <Input maxLength={1} />
        </Form.Item>
        <Form.Item
          name="currency"
          rules={[{ required: true }]}
          label={t('settings.currency')}
        >
          <Input maxLength={1} />
        </Form.Item>
        <Form.Item
          name="dateFormat"
          label={t('settings.date-format')}
          valuePropName="value"
        >
          <Select
            options={[
              { value: 'dd/MM/yy', label: format(new Date(2023, 2, 31), 'dd/MM/yy') },
              { value: 'dd/MM/yyyy', label: format(new Date(2023, 2, 31), 'dd/MM/yyyy') },
              { value: 'dd MMM yyyy', label: format(new Date(2023, 2, 31), 'dd MMM yyyy') },
              { value: 'MM/dd/yy', label: format(new Date(2023, 2, 31), 'MM/dd/yy') },
              { value: 'MM/dd/yyyy', label: format(new Date(2023, 2, 31), 'MM/dd/yyyy') },
              { value: 'MMM dd yyyy', label: format(new Date(2023, 2, 31), 'MMM dd yyyy') },
              { value: 'yy/MM/dd', label: format(new Date(2023, 2, 31), 'yy/MM/dd') },
              { value: 'yyyy-MM-dd', label: format(new Date(2023, 2, 31), 'yyyy-MM-dd') },
            ]}
          />
        </Form.Item>
        <Form.Item
          name="timeFormat"
          label={t('settings.time-format')}
          valuePropName="value"
        >
          <Select
            options={[
              { value: 'HH:mm', label: `${format(new Date(2023, 2, 31, 9, 30), 'HH:mm')} / ${format(new Date(2023, 2, 31, 21, 30), 'HH:mm')}` },
              { value: 'K:mmaaa', label: `${format(new Date(2023, 2, 31, 9, 30), 'K:mmaaa')} / ${format(new Date(2023, 2, 31, 21, 30), 'K:mmaaa')}` }
            ]}
          />
        </Form.Item>
        <Form.Item wrapperCol={{ span: 10 }}>
          <Button type="primary" htmlType="submit" block>
            {t('settings.save')}
          </Button>
        </Form.Item>
      </Form>
    </div>
  )
}
