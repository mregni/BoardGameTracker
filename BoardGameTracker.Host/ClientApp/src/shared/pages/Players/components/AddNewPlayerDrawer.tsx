import {Button, Form, Input, Upload} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {UploadOutlined} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {CreationResultType, PlayerCreation} from '../../../models';
import {PlayerContext} from '../context';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

export const AddNewPlayerDrawer = (props: Props) => {
  const { addNewPlayer, loadPlayers, addingPlayer } = useContext(PlayerContext);
  const { setOpen, open } = props;
  const { t } = useTranslation();
  const [profileImage, setProfileImage] = useState<File | null>(null);
  const [form] = Form.useForm();
  const screens = useBreakpoint();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const triggerUserCreation = async (player: PlayerCreation) => {
    const result = await addNewPlayer(player, profileImage);
    if (result === CreationResultType.Success) {
      loadPlayers();
      onClose();
    }
  }

  const handleChange = (): boolean => {
    return false;
  }

  const getFile = (e: any) => {
    setProfileImage(e.file);
    if (Array.isArray(e)) {
      return e;
    }
    return e?.fileList;
  };

  const initialValues = {
    name: '',
    image: ''
  }

  const buttonlayout = { 
    offset: screens.sm ? 4 :0, 
    span: 22 
  };

  return (
    <GcDrawer
      title={t('player.new.title')}
      open={open}
      onClose={onClose}
    >
      <Form
        form={form}
        labelCol={{ span: 4 }}
        wrapperCol={{ span: 20 }}
        layout="horizontal"
        autoComplete='off'
        onFinish={triggerUserCreation}
        initialValues={initialValues}
      >
        <Form.Item
          label={t('common.name')}
          style={{ width: '100%', marginBottom: 10 }}
          name="name"
          rules={[{ required: true, message: t('player.new.name.required') }]}
        >
          <Input disabled={addingPlayer} />
        </Form.Item>

        <Form.Item
          name="imageList"
          style={{ marginBottom: 10 }}
          label="Profile picture"
          valuePropName="fileList"
          getValueFromEvent={getFile}
        >
          <Upload
            listType="picture"
            maxCount={1}
            accept="image/*"
            beforeUpload={handleChange}>
            <Button icon={<UploadOutlined />}>Select profile picture</Button>
          </Upload>
        </Form.Item>
        <Form.Item wrapperCol={buttonlayout}>
          <Button
            type="primary"
            htmlType="submit"
            loading={addingPlayer}
            block
          >
            {t('player.new.add')}
          </Button>
        </Form.Item>
      </Form>
    </GcDrawer>
  )
}
