import * as Select from "@radix-ui/react-select";
import { cx } from "class-variance-authority";
import { useEffect, useId, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import CaretDownIcon from "@/assets/icons/caret-down.svg?react";
import CaretUpIcon from "@/assets/icons/caret-up.svg?react";
import SearchIcon from "@/assets/icons/magnifying-glass.svg?react";
import Cross from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { FormFieldWrapper } from "@/components/BgtForm";
import { useKeyboardResizeGuard } from "@/components/BgtForm/useKeyboardResizeGuard";

interface Option {
	value: number;
	label: string;
	image?: string | null;
}

interface Props {
	label: string;
	options: Option[];
	selected: number[];
	disabled: boolean;
	onChange: (selected: number[]) => void;
	placeholder?: string;
}

export const MultiSelectField = (props: Props) => {
	const { label, options, selected, onChange, placeholder, disabled } = props;
	const { t } = useTranslation("common");
	const id = useId();
	const [open, setOpen] = useState(false);
	const [searchTerm, setSearchTerm] = useState("");
	const searchInputRef = useRef<HTMLInputElement>(null);
	const isKeyboardResize = useKeyboardResizeGuard(searchInputRef);

	const filteredOptions = useMemo(
		() =>
			options.filter(
				(option) => option.label.toLowerCase().includes(searchTerm.toLowerCase()) && !selected.includes(option.value),
			),
		[options, searchTerm, selected],
	);

	const selectedItems = useMemo(() => options.filter((option) => selected.includes(option.value)), [options, selected]);

	const handleOpenChange = (isOpen: boolean) => {
		if (!isOpen && isKeyboardResize()) return;
		setOpen(isOpen);
		if (!isOpen) setSearchTerm("");
	};

	const handleSelect = (value: string) => {
		onChange([...selected, Number(value)]);
	};

	useEffect(() => {
		if (!open) return;
		const timeout = setTimeout(() => searchInputRef.current?.focus(), 0);
		return () => clearTimeout(timeout);
	}, [open]);

	return (
		<FormFieldWrapper label={label} htmlFor={id} errors={[]} className="w-full">
			<div className="flex flex-col gap-2">
				<Select.Root
					value=""
					onValueChange={handleSelect}
					open={open}
					onOpenChange={handleOpenChange}
					disabled={disabled}
				>
					<Select.Trigger
						id={id}
						className={cx(
							"w-full bg-background text-white rounded-lg border border-primary/30 focus:border-primary focus:outline-none",
							"px-4 py-2 h-11 md:h-10 inline-flex justify-between items-center text-[15px]",
							disabled && "opacity-50 cursor-not-allowed",
						)}
					>
						<span className="text-gray-400 text-sm">{placeholder}</span>
						<Select.Icon>
							{open ? <CaretUpIcon className="size-5" /> : <CaretDownIcon className="size-5" />}
						</Select.Icon>
					</Select.Trigger>
					<Select.Portal>
						<Select.Content
							position="popper"
							sideOffset={5}
							className="z-[9999] w-(--radix-select-trigger-width) overflow-hidden rounded-lg border border-primary/30 bg-input"
							onCloseAutoFocus={(event) => event.preventDefault()}
						>
							<div className="p-2 border-b border-gray-700">
								<div className="flex items-center px-2 bg-input rounded-sm">
									<SearchIcon className="size-4 text-gray-400 mr-2" />
									<input
										ref={searchInputRef}
										type="text"
										value={searchTerm}
										onChange={(event) => setSearchTerm(event.target.value)}
										onKeyDown={(event) => event.stopPropagation()}
										placeholder={t("search")}
										aria-label={t("search")}
										className="bg-transparent border-none outline-hidden py-2 text-sm w-full"
									/>
								</div>
							</div>
							<Select.Viewport className="max-h-[240px] p-1">
								{filteredOptions.length > 0 ? (
									filteredOptions.map((option) => (
										<Select.Item
											key={option.value}
											value={option.value.toString()}
											className="text-[13px] leading-none rounded-lg h-11 md:h-10 flex items-center pl-4 select-none cursor-pointer data-highlighted:bg-primary/60 data-highlighted:outline-hidden"
										>
											<Select.ItemText>
												<span className="flex flex-row justify-start items-center gap-2">
													{option.image !== undefined && <BgtAvatar title={option.label} image={option.image} />}
													{option.label}
												</span>
											</Select.ItemText>
										</Select.Item>
									))
								) : (
									<div className="text-[13px] py-2 px-4 text-gray-400">{t("no-results")}</div>
								)}
							</Select.Viewport>
						</Select.Content>
					</Select.Portal>
				</Select.Root>

				{selectedItems.length > 0 && (
					<div className="flex flex-wrap gap-2">
						{selectedItems.map((item) => (
							<div
								key={item.value}
								className="pl-1 pr-2 py-1 bg-primary/20 border border-primary/30 rounded text-sm flex items-center gap-2"
							>
								{item.image !== undefined && <BgtAvatar title={item.label} image={item.image} size="medium" />}
								<span className="text-primary">{item.label}</span>
								<button
									type="button"
									onClick={() => onChange(selected.filter((value) => value !== item.value))}
									disabled={disabled}
									className="text-primary cursor-pointer hover:text-white transition-colors py-2"
								>
									<Cross className="size-3" />
								</button>
							</div>
						))}
					</div>
				)}
			</div>
		</FormFieldWrapper>
	);
};
