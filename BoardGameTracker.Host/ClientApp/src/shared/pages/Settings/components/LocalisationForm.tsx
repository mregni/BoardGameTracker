import {Button, Form, Input} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {SettingsContext} from '../../../context/settingsContext';
import {Settings} from '../../../models';

export const LocalisationForm = () => {
  const { t } = useTranslation();
  const { settings, saveSettings } = useContext(SettingsContext);
  const [form] = Form.useForm();

  const onFinish = async (values: Settings) => {
    console.log(values);
    await saveSettings({
      ...settings,
      decimalSeparator: values.decimalSeparator,
      currency: values.currency
    });
  }
  return (
    <div style={{ width: 400 }}>
      <h3>Localisation</h3>
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
        <Form.Item wrapperCol={{ span: 10 }}>
          <Button type="primary" htmlType="submit" block>
            {t('settings.save')}
          </Button>
        </Form.Item>
      </Form>
    </div>
  )
}
