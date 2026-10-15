import * as Select from "@radix-ui/react-select";
import type { AnyFieldApi } from "@tanstack/react-form";
import { cx } from "class-variance-authority";
import { memo, useCallback, useEffect, useId, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import CheckIcon from "@/assets/icons/check.svg?react";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import type { BgtSelectImageItem, BgtSelectItem } from "@/models";
import { BgtAvatar } from "../BgtAvatar/BgtAvatar";
import { FormFieldWrapper } from "./FormFieldWrapper";
import { useKeyboardResizeGuard } from "./useKeyboardResizeGuard";

type SelectValue = number | string;

interface FieldBinding {
	field: AnyFieldApi;
	value?: never;
	onValueChange?: never;
	errors?: never;
}

interface ControlledBinding {
	field?: never;
	value: SelectValue | null;
	onValueChange: (value: SelectValue) => void;
	errors?: string[];
}

export interface BgtSelectBaseProps {
	label: string;
	items: BgtSelectImageItem[] | BgtSelectItem[];
	disabled?: boolean;
	placeholder?: string;
	hasSearch?: boolean;
	showAvatars?: boolean;
}

export type BgtSelectProps = (FieldBinding | ControlledBinding) & BgtSelectBaseProps;

type Item = BgtSelectImageItem | BgtSelectItem;

const groupItems = (items: Item[]) =>
	items.reduce<{ label: string; items: Item[] }[]>((groups, item) => {
		const label = item.group ?? "";
		const group = groups.find((x) => x.label === label);
		if (group) {
			group.items.push(item);
		} else {
			groups.push({ label, items: [item] });
		}
		return groups;
	}, []);

const BgtSelectComponent = (props: BgtSelectProps) => {
	const { items, label, disabled = false, placeholder = null, hasSearch = false, showAvatars = false } = props;

	const { t } = useTranslation();
	const id = useId();
	const [searchTerm, setSearchTerm] = useState("");
	const [open, setOpen] = useState(false);
	const searchInputRef = useRef<HTMLInputElement>(null);
	const triggerRef = useRef<HTMLButtonElement>(null);
	const isKeyboardResize = useKeyboardResizeGuard(searchInputRef);

	const errors: string[] = props.field ? props.field.state.meta.errors : (props.errors ?? []);
	const rawValue = (props.field ? props.field.state.value : props.value)?.toString();
	const currentValue = items.some((item) => item.value.toString() === rawValue) ? rawValue : "";

	const filteredItems = useMemo(() => {
		return items.filter((item) => {
			if (item.value.toString() === currentValue) {
				return true;
			}
			return item.label.toLowerCase().includes(searchTerm.toLowerCase());
		});
	}, [items, currentValue, searchTerm]);

	const visibleGroups = useMemo(
		() =>
			searchTerm === "" && filteredItems.some((item) => item.group)
				? groupItems(filteredItems)
				: [{ label: "", items: filteredItems }],
		[filteredItems, searchTerm],
	);

	const isSelectImageItem = useCallback(
		(item: BgtSelectImageItem | BgtSelectItem): item is BgtSelectImageItem =>
			item && typeof item === "object" && "image" in item && (showAvatars || item.image !== null),
		[showAvatars],
	);

	const selectedHasImage = useMemo(() => {
		const selected = items.find((item) => item.value.toString() === currentValue);
		return !!selected && isSelectImageItem(selected);
	}, [items, currentValue, isSelectImageItem]);

	const hasErrors = errors.length > 0;
	const { field, onValueChange } = props;

	const handleValueChange = useCallback(
		(value: string) => {
			const originalItem = items.find((item) => item.value.toString() === value);
			const next = originalItem ? originalItem.value : value;
			if (field) {
				field.handleChange(next);
			} else {
				onValueChange?.(next);
			}
		},
		[field, onValueChange, items],
	);

	const handleOpenChange = useCallback(
		(isOpen: boolean) => {
			if (!isOpen && isKeyboardResize()) {
				return;
			}
			setOpen(isOpen);
			if (!isOpen) {
				setSearchTerm("");
			}
		},
		[isKeyboardResize],
	);

	const handleSearchChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
		setSearchTerm(e.target.value);
	}, []);

	const handleSearchKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
		if (e.key === "ArrowDown") {
			e.preventDefault();
			e.currentTarget
				.closest('[role="listbox"]')
				?.querySelector<HTMLElement>('[role="option"]:not([data-disabled])')
				?.focus();
		}
		e.stopPropagation();
	}, []);

	useEffect(() => {
		if (open && searchInputRef.current) {
			setTimeout(() => {
				searchInputRef.current?.focus();
			}, 0);
		}
	}, [open]);

	return (
		<FormFieldWrapper label={label} htmlFor={id} errors={errors}>
			<Select.Root
				disabled={disabled}
				onValueChange={handleValueChange}
				value={currentValue}
				open={open}
				onOpenChange={handleOpenChange}
			>
				<Select.Trigger
					ref={triggerRef}
					id={id}
					className={cx(
						"w-full bg-background text-white border border-primary/30 focus:border-primary focus:outline-none",
						"py-2 h-11 md:h-10 shadow-none inline-flex justify-between items-center rounded-lg leading-none text-[15px]",
						selectedHasImage ? "pl-2 pr-4" : "px-4",
						hasErrors && "border border-error bg-error-dark!",
						disabled && "opacity-50 cursor-not-allowed",
					)}
				>
					<Select.Value placeholder={placeholder} />
					<Select.Icon>{open ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}</Select.Icon>
				</Select.Trigger>
				<Select.Portal>
					<Select.Content
						className="overflow-hidden bg-input rounded-md w-(--radix-select-trigger-width) z-[9999]"
						position="popper"
						sideOffset={5}
						style={{ bottom: "auto" }}
						onCloseAutoFocus={(e) => {
							e.preventDefault();
							triggerRef.current?.focus({ preventScroll: true });
						}}
					>
						{hasSearch && (
							<div className="p-2 border-b border-gray-700">
								<div className="flex items-center px-2 bg-input rounded-sm">
									<SearchIcon className="size-4 text-gray-400 mr-2" />
									<input
										ref={searchInputRef}
										type="text"
										value={searchTerm}
										onChange={handleSearchChange}
										placeholder={t("search")}
										aria-label={t("search")}
										className="bg-transparent border-none outline-hidden py-2 text-sm w-full"
										onClick={(e) => e.stopPropagation()}
										onKeyDown={handleSearchKeyDown}
									/>
								</div>
							</div>
						)}
						<Select.Viewport className="p-1 max-h-[300px]">
							{filteredItems.length > 0 ? (
								visibleGroups.map((group) => (
									<Select.Group key={group.label}>
										{group.label && (
											<Select.Label className="px-[25px] pt-2 pb-1 text-[11px] uppercase tracking-wider text-gray-400">
												{group.label}
											</Select.Label>
										)}
										{group.items.map((item) => (
											<Select.Item
												value={item.value.toString()}
												key={item.value}
												className="text-[13px] leading-none rounded-lg h-11 md:h-10 flex items-center pr-[35px] pl-[25px] relative select-none data-disabled:text-mauve8 data-disabled:pointer-events-none data-highlighted:outline-hidden data-highlighted:bg-primary/60 hover:cursor-pointer"
											>
												<Select.ItemText>
													<div className="flex flex-row justify-start items-center gap-2">
														{isSelectImageItem(item) && <BgtAvatar title={item.label} image={item.image} />}
														{item.label}
													</div>
												</Select.ItemText>
												<Select.ItemIndicator className="absolute left-0 w-[25px] inline-flex items-center justify-center">
													<CheckIcon className="size-4" />
												</Select.ItemIndicator>
											</Select.Item>
										))}
									</Select.Group>
								))
							) : (
								<div className="text-[13px] py-2 px-4 text-gray-400">{t("no-results")}</div>
							)}
						</Select.Viewport>
					</Select.Content>
				</Select.Portal>
			</Select.Root>
		</FormFieldWrapper>
	);
};

BgtSelectComponent.displayName = "BgtSelect";

export const BgtSelect = memo(BgtSelectComponent);
