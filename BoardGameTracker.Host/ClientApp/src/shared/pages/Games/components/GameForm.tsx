import {Button, DatePicker, Form, FormInstance, Input, InputNumber, Select, Space} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {SettingsContext} from '../../../context/settingsContext';
import {useScreenInfo} from '../../../hooks/useScreenInfo';
import {Game, GameState, getItemStateTranslationKey} from '../../../models';
import {convertToAntdFormat} from '../../../utils';

interface FormProps {
  form: FormInstance<Game>;
  submitAction: (values: Game) => void;
  initialValues: Game | undefined;
  loading: boolean;
}

export const GameForm = (props: FormProps) => {
  const { form, submitAction, initialValues, loading } = props;
  const { t } = useTranslation();
  const { screenMap } = useScreenInfo();
  const { settings } = useContext(SettingsContext);

  const buttonlayout = {
    offset: screenMap.sm ? 4 : 0,
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
        style={{ width: '100%', marginBottom: 10 }}
        name="name"
        rules={[{ required: true, message: t('game.new.name.required') }]}
      >
        <Input disabled={loading} placeholder={t('common.name')} />
      </Form.Item>

      <Form.Item
        label={t('common.state')}
        name="state"
        style={{ marginBottom: 10 }}
      >
        <Select
          disabled={loading}
          options={Object
            .keys(GameState)
            .filter(value => !isNaN(Number(value)))
            .map((value) => ({ label: t(getItemStateTranslationKey(value)), value: Number(value) }))}
        >
        </Select>
      </Form.Item>

      <Form.Item
        label={t('game.price')}
        name="price"
        style={{ marginBottom: 10 }}
      >
        <InputNumber
          addonAfter={settings.currency}
          disabled={loading}
          placeholder={t('game.price-placeholder')}
          controls={false}
          style={{ width: '100%' }}
        />
      </Form.Item>

      <Form.Item
        label={t('game.added-date')}
        name="additionDate"
        style={{ marginBottom: 10 }}
      >
        <DatePicker
          style={{ width: '100%' }}
          disabled={loading}
          format={`${convertToAntdFormat(settings?.dateFormat)}`}
        />
      </Form.Item>

      <Form.Item
        name="yearPublished"
        label={t('common.published')}
      >
        <InputNumber
          placeholder={t('common.year')}
          style={{ width: '100%' }}
        />
      </Form.Item>

      <Form.Item name="description" label={t('common.description')}>
        <Input.TextArea rows={4} />
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
        <InputNumber placeholder={t('common.min')} style={{ width: '100%' }} />
      </Form.Item>

      <Form.Item label={t('common.rating')} name="rating">
        <InputNumber addonAfter="/10" style={{ width: '100%' }} />
      </Form.Item>

      <Form.Item label={t('common.weight')} name="weight">
        <InputNumber 
        addonAfter="/5" 
        placeholder={t('common.complexity')}
        style={{ width: '100%' }} 
        />
      </Form.Item>

      <Form.Item label={t('games.new.bgg.label')} name="bggId">
        <InputNumber style={{ width: '100%' }} />
      </Form.Item>

      <Form.Item wrapperCol={buttonlayout}>
        <Button
          type="primary"
          htmlType="submit"
          loading={loading}
          block
        >
          {t('game.save')}
        </Button>
      </Form.Item>
    </Form>
  )
}
