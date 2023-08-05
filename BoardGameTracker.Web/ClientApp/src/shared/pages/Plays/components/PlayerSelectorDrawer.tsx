import {Button, Checkbox, Col, Divider, Form, Input, Row, Select, Space} from 'antd';
import React, {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {DeleteOutlined} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {ActivePlayer} from '../../../models';
import {PlayerContext} from '../../Players/context';

const { Option } = Select;

type Props = {
  open: boolean;
  close: (player: ActivePlayer | null) => void;
  edit: ActivePlayer | undefined;
  nextId: number;
}

const PlayerSelectorDrawer = (props: Props) => {
  const { close, open, edit, nextId } = props;
  const { players, loading: loadingPlayers } = useContext(PlayerContext);
  const { t } = useTranslation();
  const [form] = Form.useForm();

  useEffect(() => {
    if(edit !== undefined) {
      form.setFieldValue('playerId', edit.playerId);
      form.setFieldValue('won', edit.won);
      form.setFieldValue('firstPlay', edit.firstPlay);
      form.setFieldValue('color', edit.color);
      form.setFieldValue('score', edit.score);
      form.setFieldValue('team', edit.team);
      form.setFieldValue('characterName', edit.characterName);
    }
  }, [edit, form]);
  

  const formItemLayout = {
    labelCol: { span: 4 },
    wrapperCol: { span: 18 }
  };

  const onClose = (player: ActivePlayer | null) => {
    form.resetFields();
    close(player);
  };

  const triggerPlaySave = async (player: ActivePlayer) => {
    player.uiId = edit?.uiId ?? nextId;
    onClose(player);
    return '';
  }

  return (
    <GcDrawer
      open={open}
      onClose={() => onClose(null)}
      title="Add player">
      <Form
        form={form}
        {...formItemLayout}
        layout="horizontal"
        onFinish={triggerPlaySave}
        autoComplete='off'
        hideRequiredMark
      >
        <Form.Item
          style={{ marginBottom: 10 }}
          name="playerId"
          label={t('common.players')}
          valuePropName="value"
          rules={[{ required: true, message: t('play.new.player.required') }]}
        >
          <Select
            placeholder={t('play.new.player.placeholder')}
            loading={loadingPlayers}
            showSearch
            filterOption={(input, option) => ((option?.children ?? '') as string).toLowerCase().includes(input.toLowerCase())}
          >
            {players.map(x => <Option key={x.id} value={x.id} >{x.name}</Option>)}
          </Select>
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 10 }}
          label="Scoring"
          name="score"
          wrapperCol={{ span: 7 }}
        >
          <Input placeholder="End scoring" />
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 10 }}
          label="Color"
          name="color"
          wrapperCol={{ span: 7 }}
        >
          <Input placeholder="Player Color" />
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 10 }}
          label="Team"
          name="team"
          wrapperCol={{ span: 7 }}
        >
          <Input placeholder="Team name" />
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 10 }}
          label="Character name"
          name="characterName"
        >
          <Input />
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 5 }}
          label="Won"
          name="won"
          valuePropName="checked"
        >
          <Checkbox />
        </Form.Item>
        <Form.Item
          style={{ marginBottom: 5 }}
          label="First play"
          name="firstPlay"
          valuePropName="checked"
        >
          <Checkbox />
        </Form.Item>
        <Row justify="end">
          <Button type="primary" htmlType="submit">{edit === undefined ? "Add player": "Edit player"}</Button>
        </Row>
      </Form>
    </GcDrawer>
  )
}

export default PlayerSelectorDrawer