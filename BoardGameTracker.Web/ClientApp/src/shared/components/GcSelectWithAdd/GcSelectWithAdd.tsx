import {Button, Col, Divider, Input, InputRef, Row, Select, Space} from 'antd';
import React, {useEffect, useRef, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {PlusOutlined} from '@ant-design/icons';

let index = 0;

type Props = {
  items: string[];
  buttonKey: string;
  placeholderKey: string;
  handleChange: (value: string) => void;
}

export const GcSelectWithAdd = (props: Props) => {
  const { items, buttonKey, placeholderKey, handleChange } = props;
  const [internalItems, setInternalItems] = useState<string[]>([]);
  const [selected, setSelected] = useState<string>("")
  const [name, setName] = useState('');
  const inputRef = useRef<InputRef>(null);
  const { t } = useTranslation();

  useEffect(() => {
    setInternalItems(items);
    const value = items[0].length > 0 ? items[0] : "";
    setSelected(value);
    handleChange(value);
    //TODO: Check why adding handlChange to dep array gives enless refresh. maybe look at useCallback in the parent component.
  }, [items]);

  const onNameChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    setName(event.target.value);
  };

  const addItem = (e: React.MouseEvent<HTMLButtonElement | HTMLAnchorElement>) => {
    e.preventDefault();
    setInternalItems([...internalItems, name || `New item ${index++}`]);
    setName('');
    setTimeout(() => {
      inputRef.current?.focus();
    }, 0);
  };

  const handleInternalChange = (value: string) => {
    setSelected(value);
    handleChange(value);
  }

  return (
    <Select
      style={{ width: "calc(100% - 10px)" }}
      value={selected}
      allowClear
      onChange={handleInternalChange}
      options={internalItems.map((item) => ({ label: item, value: item }))}
      dropdownRender={(menu) => (
        <>
          {menu}
          <Divider style={{ margin: '8px 0' }} />
          <Row style={{ padding: '0 8px 4px'}} gutter={8}>
            <Col flex="auto">
              <Input
                placeholder={t(placeholderKey)}
                ref={inputRef}
                value={name}
                onChange={onNameChange}
                style={{ flexGrow: 1 }}
              /></Col>
            <Col>
              <Button type="text" icon={<PlusOutlined />} onClick={addItem} >
                {t(buttonKey)}
              </Button>
            </Col>
          </Row>
        </>
      )}
    />
  )
}
