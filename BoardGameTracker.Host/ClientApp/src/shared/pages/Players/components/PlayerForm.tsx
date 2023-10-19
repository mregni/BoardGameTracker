import {Button, Form, FormInstance, Input, Upload, UploadFile} from 'antd';
import {useCallback, useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

import {useScreenInfo} from '../../../hooks/useScreenInfo';
import {PlayerContext} from '../context';

export interface FormPlayer {
  id: number;
  name: string;
  fileList: UploadFile[];
}

interface FormProps {
  form: FormInstance<FormPlayer>;
  submitAction: (values: FormPlayer) => void;
  initialValues: FormPlayer;
  loading: boolean;
}

export const PlayerForm = (props: FormProps) => {
  const { form, submitAction, initialValues, loading } = props;
  const { t } = useTranslation();
  const { screenMap } = useScreenInfo();

  const buttonlayout = {
    offset: screenMap.sm ? 4 : 0,
    span: 22
  };

  const getFile = useCallback(
    (e: { file: UploadFile, fileList: UploadFile[] }) => {
      if (Array.isArray(e)) {
        return e;
      }
      return e?.fileList;
    },
    []
  );

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
        style={{ width: '100%', marginBottom: 10 }}
        name="name"
        rules={[{ required: true, message: t('player.new.name.required') }]}
      >
        <Input disabled={loading} />
      </Form.Item>
      <Form.Item
        name="fileList"
        style={{ marginBottom: 10 }}
        label={t('common.profile-picture')}
        valuePropName="fileList"
        getValueFromEvent={getFile}
      >
        <Upload
          listType="picture-card"
          maxCount={1}
          accept="image/*"
          disabled={loading}
          showUploadList={{
            showPreviewIcon: false
          }}
          beforeUpload={() => false}>
          <div>
            <PlusOutlined />
            <div style={{ marginTop: 8 }}>{t('common.select')}</div>
          </div>
        </Upload>
      </Form.Item>
      <Form.Item wrapperCol={buttonlayout}>
        <Button
          type="primary"
          htmlType="submit"
          loading={loading}
          block
        >
          {t('player.save')}
        </Button>
      </Form.Item>
    </Form>
  )
}