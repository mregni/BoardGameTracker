import {Button, Form, Input, InputNumber, Space, Upload} from 'antd';
import React, {useMemo, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

import {BggGame, Game} from '../../models';
import {GcSelectWithAdd} from '../Gc/GcSelectWithAdd';

type Props = {
  rawGame: BggGame;
}

const NewGameFormDrawer = (props: Props) => {
  const { t } = useTranslation();
  const { rawGame } = props;
  const [game, setGame] = useState<Game>(null!);

  const handleTitleChange = (value: string): void => {
    setGame(prev => ({ ...prev, title: value }));
  }

  const nameList = useMemo(
    () => rawGame.names,
    [rawGame.names]
  );

  const normFile = (e: any) => {
    if (Array.isArray(e)) {
      return e;
    }
    return e?.fileList;
  };

  const onFinish = (values: Game) => {
    const newGame: Game = {
      ...values
    }

    if (rawGame.names.length > 0) {
      newGame.title = game.title;
    }

    console.log(newGame);
  };

  return (
    <Form
      labelCol={{ span: 4 }}
      wrapperCol={{ span: 19 }}
      layout="horizontal"
      initialValues={rawGame}
      autoComplete='off'
      onFinish={onFinish}>
      {rawGame && rawGame.names.length > 0
        ? (
          <Form.Item label={t('common.title')} name="title">
            <GcSelectWithAdd
              items={nameList}
              buttonKey='games.new.bgg.new-title-button'
              placeholderKey='games.new.bgg.new-title'
              handleChange={handleTitleChange}
            />
          </Form.Item>
        ) :
        (
          <Form.Item name="title" label={t('common.title')}>
            <Input />
          </Form.Item>
        )}

      <Form.Item name="yearPublished" label={t('common.published')}>
        <InputNumber placeholder={t('common.year')} />
      </Form.Item>

      <Form.Item name="description" label={t('common.description')}>
        <Input.TextArea showCount rows={7} />
      </Form.Item>

      <Form.Item label={t('common.players')}>
        <Space.Compact>
          <Form.Item name='minPlayers' noStyle>
            <InputNumber placeholder={t('common.min')} />
          </Form.Item>
          <Form.Item name='maxPlayers' noStyle>
            <InputNumber placeholder={t('common.max')} />
          </Form.Item>
        </Space.Compact>
      </Form.Item>

      <Form.Item label={t('common.playtime')}>
        <Space.Compact>
          <Form.Item name='minPlayTime' noStyle>
            <InputNumber placeholder={t('common.min')} />
          </Form.Item>
          <Form.Item name='maxPlayTime' noStyle>
            <InputNumber placeholder={t('common.max')} />
          </Form.Item>
        </Space.Compact>
      </Form.Item>

      <Form.Item label={t('common.minage')} name="minAge">
        <InputNumber placeholder={t('common.min')} />
      </Form.Item>

      <Form.Item label={t('common.rating')} name="rating">
        <InputNumber addonAfter="/10" />
      </Form.Item>

      <Form.Item label={t('games.new.bgg.label')} name="bggId">
        <InputNumber />
      </Form.Item>

      <Form.Item label='Upload' valuePropName='fileList' name='test' getValueFromEvent={normFile}>
        <Upload action="https://localhost:7178/api/game/image" listType="picture-card" maxCount={1} accept="image/*">
          <div>
            <PlusOutlined />
            <div style={{ marginTop: 8 }}>{t('common.upload')}</div>
          </div>
        </Upload>
      </Form.Item>

      <Form.Item wrapperCol={{ span: 24 }}>
        <Button type="primary" htmlType="submit" block>
          {t('games.new.submit')}
        </Button>
      </Form.Item>
    </Form>
  );
}

export default NewGameFormDrawer