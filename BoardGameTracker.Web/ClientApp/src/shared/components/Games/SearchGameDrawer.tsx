import {Button, Drawer, Form, Input, Select, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React from 'react';
import {useTranslation} from 'react-i18next';

import {BggIdSearch, getItemStateTranslationKey, ItemState, SearchResult} from '../../models';
import useSearchGame from './useSearchGame';

type Props = {
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  open: boolean;
}

const SearchGameDrawer = (props: Props) => {
  const { setOpen, open } = props;
  const {t} = useTranslation();
  const {search, searching} = useSearchGame();
  const [form] = Form.useForm();
  const screens = useBreakpoint();

  const onClose = () => {
    form.resetFields();
    setOpen(false);
  };

  const triggerGameSearch = async (values: BggIdSearch) => {
    const result = await search(values.bggId);
    console.log(result);
    if(![SearchResult.Failed, SearchResult.NotFound].includes(result)){
      onClose();
    }
  }

  const getDrawerWith = () => {
    return screens.md ? 700 : '100%';
  }

  const initialValues = {
    bggId: '',
    state: ItemState.Owned
  }

  return (
    <Drawer
      title={t('games.new.title')}
      onClose={onClose}
      closable={false}
      width={getDrawerWith()}
      open={open}
      bodyStyle={{ paddingBottom: 80 }}
      extra={
        <Space>
          <Button onClick={onClose}>{t('common.cancel')}</Button>
        </Space>
      }
    >
      <Form
        form={form}
        labelCol={{ span: 4 }}
        wrapperCol={{ span: 19 }}
        layout="horizontal"
        autoComplete='off'
        hideRequiredMark
        onFinish={triggerGameSearch}
        initialValues={initialValues}
      >
        <Form.Item
          label={t('games.new.bgg.label')}
          style={{ width: '100%' }}
          name="bggId"
          rules={[{ required: true, message: t('games.new.bgg.required') }]}
        >
          <Input placeholder={t('games.new.bgg.placeholder')} disabled={searching} />
        </Form.Item>
        <Form.Item
          label={t('common.state')}
          style={{ width: '100%' }}
          name="state"
        >
          <Select
            disabled={searching}
            options={Object
              .keys(ItemState)
              .filter(value => !isNaN(Number(value)))
              .map((value) => ({ label: t(getItemStateTranslationKey(value)), value: Number(value) }))}
          >
          </Select>
        </Form.Item>
        <Form.Item wrapperCol={{ offset: 1, span: 22 }}>
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
    </Drawer >
  );
}

export default SearchGameDrawer