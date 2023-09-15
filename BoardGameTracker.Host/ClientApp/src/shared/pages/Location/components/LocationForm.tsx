import {Button, Form, FormInstance, Input} from 'antd';
import React, {useEffect} from 'react';
import {useTranslation} from 'react-i18next';

import {useScreenInfo} from '../../../hooks/useScreenInfo';
import {FormLocation} from '../../../models';

type Props = {
  form: FormInstance<FormLocation>;
  submitAction: (values: FormLocation) => void;
  initialValues: FormLocation;
}

export const LocationForm = (props: Props) => {
  const { form, submitAction, initialValues } = props;
  const { t } = useTranslation();
  const { screenMap } = useScreenInfo();

  useEffect(() => {
    form.resetFields()
  }, [initialValues, form]);

  const buttonlayout = { 
    offset: screenMap.sm ? 4 :0, 
    span: 22 
  };


  return (
    <Form
      form={form}
      labelCol={{ span: 4 }}
      wrapperCol={{ span: 20 }}
      layout="horizontal"
      autoComplete='off'
      onFinish={submitAction}
      initialValues={initialValues}
    >
      <Form.Item
        label={t('common.name')}
        style={{ marginBottom: 10 }}
        name="name"
        rules={[{ required: true, message: t('location.new.name.required') }]}
      >
        <Input />
      </Form.Item>
      <Form.Item wrapperCol={buttonlayout}>
        <Button
          type="primary"
          htmlType="submit"
          block
        >
          {t('location.new.save')}
        </Button>
      </Form.Item>
    </Form>
  )
}
