import {Button, DatePicker, Form, Input, InputNumber, Select, Space} from 'antd';
import React, {useContext} from 'react';
import {useTranslation} from 'react-i18next';

import {ExportOutlined} from '@ant-design/icons';

import {GcDrawer} from '../../../components/GcDrawer';
import {SettingsContext} from '../../../context/settingsContext';
import {useScreenInfo} from '../../../hooks/useScreenInfo';
import {BggSearch, GameState, getItemStateTranslationKey, SearchResultType} from '../../../models';
import {convertToAntdFormat} from '../../../utils';
import {GamesContext} from '../context';
import useSearchGame from '../useSearchGame';

interface Props {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

const SearchGameDrawer = (props: Props) => {
  const { loadGames } = useContext(GamesContext);
  const { settings } = useContext(SettingsContext);
  const { setOpen, open } = props;
  const { t } = useTranslation();
  const { search, searching } = useSearchGame();
  const [form] = Form.useForm();
  const { screenMap } = useScreenInfo();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const triggerGameSearch = async (values: BggSearch) => {
    const result = await search(values);
    if (![SearchResultType.Failed, SearchResultType.NotFound].includes(result)) {
      loadGames();
      onClose();
    }
  }

  const initialValues = {
    bggId: '',
    state: GameState.Owned,
    price: null,
    dateAdded: null
  }

  const openBgg = () => {
    window.open('https://boardgamegeek.com/browse/boardgame', '_blank');
  }

  if (settings === null) {
    return (<></>)
  }

  const buttonlayout = { 
    offset: screenMap.sm ? 4 :0, 
    span: 22 
  };

  return (
    <GcDrawer
      title={t('games.new.title')}
      open={open}
      onClose={onClose}>
      <Form
        form={form}
        labelCol={{ span: 4 }}
        wrapperCol={{ span: 20 }}
        layout="horizontal"
        autoComplete='off'
        onFinish={triggerGameSearch}
        initialValues={initialValues}
      >
        <Form.Item
          label={t('games.new.bgg.label')}
          name="bggId"
          style={{ marginBottom: 10 }}
          rules={[{ required: true, message: t('games.new.bgg.required') }]}
        >
          <Space.Compact style={{ width: '100%' }}>
            <Input placeholder={t('games.new.bgg.placeholder')} disabled={searching} />
            <Button type="primary" icon={<ExportOutlined />} disabled={searching} onClick={openBgg}>{t('games.open-bgg')}</Button>
          </Space.Compact>
        </Form.Item>
        <Form.Item
          label={t('common.state')}
          name="state"
          style={{ marginBottom: 10 }}
        >
          <Select
            disabled={searching}
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
            disabled={searching}
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
            disabled={searching}
            format={`${convertToAntdFormat(settings?.dateFormat)}`}
          />
        </Form.Item>
        <Form.Item wrapperCol={buttonlayout}>
          <Button
            type="primary"
            htmlType="submit"
            loading={searching}
            block
          >
            {t('games.new.bgg.search')}
          </Button>
        </Form.Item>
      </Form>
    </GcDrawer>
  );
}

export default SearchGameDrawer