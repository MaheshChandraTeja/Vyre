function(vyre_enable_clang_tidy)
  find_program(CLANG_TIDY_EXE NAMES clang-tidy)
  if(NOT CLANG_TIDY_EXE)
    message(WARNING "clang-tidy requested but not found.")
    return()
  endif()

  set(CMAKE_CXX_CLANG_TIDY
    ${CLANG_TIDY_EXE};
    --config-file=${CMAKE_SOURCE_DIR}/.clang-tidy
  )

  message(STATUS "clang-tidy enabled: ${CLANG_TIDY_EXE}")
endfunction()
